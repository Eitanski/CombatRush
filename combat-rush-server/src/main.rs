mod units;
mod build;

use anyhow::Result;
use std::{collections::HashMap, net::SocketAddr, sync::Arc};
use tokio::sync::mpsc::{UnboundedReceiver, UnboundedSender};
use tokio::{
    io::{AsyncReadExt, AsyncWriteExt},
    net::{TcpListener, TcpStream},
    sync::{mpsc, Mutex},
};
use tokio::net::tcp::{OwnedReadHalf, OwnedWriteHalf};
use units::*;

#[derive(Default)]
struct ServerState {
    // map of connected peers -> their outbound channel
    peers: HashMap<SocketAddr, UnboundedSender<Vec<u8>>>,
    units: Vec<Worker>,
}

#[tokio::main]
async fn main() -> Result<()> {
    println!("Initiating Combat Rush server");
    let addr = "0.0.0.0:7000";
    let listener = TcpListener::bind(addr).await?;
    println!("server listening on {addr}");

    let state = Arc::new(Mutex::new(ServerState::default()));

    let (processing_sender, processing_receiver) = mpsc::unbounded_channel::<Vec<u8>>();

    let state_for_server = Arc::clone(&state);
    tokio::spawn(async move { server_logic(state_for_server, processing_receiver).await });

    loop {
        let (stream, peer_addr) = listener.accept().await?;
        println!("new client joined at {peer_addr}");
        let client_state = Arc::clone(&state);
        let sender_clone = processing_sender.clone();
        tokio::spawn(async move {
            if let Err(e) = handle_client(stream, peer_addr, client_state, sender_clone).await {
                eprintln!("[{peer_addr}] error: {e:?}");
            }
        });
    }
}

async fn handle_client(
    stream: TcpStream,
    peer: SocketAddr,
    state: Arc<Mutex<ServerState>>,
    processing_sender: UnboundedSender<Vec<u8>>,
) -> Result<()> {
    // set up per-connection outbound channel
    let (sender, receiver) = mpsc::unbounded_channel::<Vec<u8>>();
    {
        let mut s = state.lock().await;
        s.peers.insert(peer, sender);
    }

    let (reader, writer) = stream.into_split();

    // spawn writer task
    let writer = tokio::spawn(async move {
        if let Err(e) = writer_loop(writer, receiver).await {
            eprintln!("[{peer}] writer_loop error: {e:?}");
        }
    });

    // reader loop
    let res = reader_loop(reader, &processing_sender).await;

    // cleanup on disconnect
    {
        let mut s = state.lock().await;
        s.peers.remove(&peer);
        // TODO: remove player entities, free resources, etc.
    }

    // drop writer (closes when rx is dropped)
    writer.abort();
    res
}

async fn server_logic(state: Arc<Mutex<ServerState>>, mut receiver: UnboundedReceiver<Vec<u8>>) {
    loop {
        let _ = receiver.recv().await.unwrap();
        {
            let clients = &state.lock().await.peers;
            broadcast(vec![], clients).await;
        }
    }
}

async fn broadcast(transmission: Vec<u8>, clients: &HashMap<SocketAddr, UnboundedSender<Vec<u8>>>) {
    clients.values().for_each(|sender| sender.send(transmission.clone()).unwrap());
}

async fn reader_loop(
    mut stream_reader: OwnedReadHalf,
    processing_sender: &UnboundedSender<Vec<u8>>,
) -> Result<()> {
    loop {
        let frame = read_frame(&mut stream_reader).await?;

        send_to_server_logic(frame, processing_sender).await?;
    }
}

async fn writer_loop(mut stream_writer: OwnedWriteHalf, mut receiver: UnboundedReceiver<Vec<u8>>) -> Result<()> {
    while let Some(buf) = receiver.recv().await {
        write_frame(&mut stream_writer, &buf).await?;
    }
    Ok(())
}

// --------------- small helpers ---------------

async fn send_to_server_logic(payload: Vec<u8>, processing_sender: &UnboundedSender<Vec<u8>>) -> Result<()> {
    processing_sender.send(payload)?;
    Ok(())
}

// length-prefixed frame: [u32 BE length][payload bytes]
async fn read_frame(stream_reader: &mut OwnedReadHalf) -> Result<Vec<u8>> {
    let mut len_buf = [0u8; 4];
    stream_reader.read_exact(&mut len_buf).await?;
    let len = u32::from_be_bytes(len_buf) as usize;

    let mut payload = vec![0u8; len];
    stream_reader.read_exact(&mut payload).await?;
    Ok(payload)
}

async fn write_frame(stream_writer: &mut OwnedWriteHalf, payload: &[u8]) -> Result<()> {
    let len = (payload.len() as u32).to_be_bytes();
    stream_writer.write_all(&len).await?;
    stream_writer.write_all(payload).await?;
    stream_writer.flush().await?;
    Ok(())
}
