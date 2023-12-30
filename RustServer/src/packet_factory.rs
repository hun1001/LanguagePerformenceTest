use log::debug;

use crate::packet::Packet;

#[derive(Debug)]
pub struct PacketFactory {
    buffer: Vec<u8>,
    packet: Option<Packet>,
}

impl PacketFactory {
    pub fn new() -> Self {
        Self {
            buffer: Vec::new(),
            packet: None,
        }
    }

    pub fn push(&mut self, data: &[u8]) {
        self.buffer.extend_from_slice(data);

        if let Some(index) = self.buffer.iter().position(|&x| x == 0) {
            let packet = Packet::deserialize(&self.buffer[..index]);
            self.packet = Some(packet);
            self.buffer.drain(..index + 1);
        }
    }

    pub fn next(&mut self) -> Option<Packet> {
        let packet = self.packet.take();
        self.packet = None;
        packet
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_packet_factory() {
        let mut packet_factory = PacketFactory::new();

        let packet = Packet {
            user_id: String::from("test"),
            time_stamp: String::from("test"),
            message: String::from("test"),
        };

        let serialized_packet = packet.serialize();

        packet_factory.push(&serialized_packet);

        while let Some(packet) = packet_factory.next() {
            debug!("{:?}", packet);
        }
    }

    #[test]
    fn test_packet_factory_100() {
        let mut packet_factory = PacketFactory::new();

        for _ in 0..100 {
            let packet = Packet {
                user_id: String::from("test"),
                time_stamp: String::from("test"),
                message: String::from("test"),
            };

            let serialized_packet = packet.serialize();

            packet_factory.push(&serialized_packet);
        }

        while let Some(packet) = packet_factory.next() {
            debug!("{:?}", packet);
        }
    }
}