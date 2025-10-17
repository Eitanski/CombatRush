use glam::Vec2;

#[derive(Default)]
#[derive(Debug)]
pub struct Worker {
    pos: Vec2,
    id: String,
}

impl Worker {
    pub fn new(pos: Vec2, id: String) -> Self {
        Self { pos, id }
    }
}