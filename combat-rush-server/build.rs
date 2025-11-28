use std::path::{Path, PathBuf};

fn main() {
    let proto_root = PathBuf::from("../proto"); 
    let envelope = proto_root.join(String::from("combat_rush/v1/envelope.proto"));

    // Rebuild if the file changes
    println!("cargo:rerun-if-changed={}", envelope.display());

    prost_build::Config::new()
        .compile_protos(&[envelope], &[proto_root])
        .unwrap();
}
