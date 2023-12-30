use bytes::{Bytes, BytesMut};
use log::error;

#[derive(Debug, Clone)]
pub struct Packet {
    pub user_id: String,
    pub time_stamp: String,
    pub message: String,
}

impl Packet {
    pub fn serialize(&self) -> Bytes {
        let mut buf = BytesMut::new();
        buf.extend_from_slice(self.user_id.as_bytes());
        buf.extend_from_slice("|".as_bytes());
        buf.extend_from_slice(self.time_stamp.as_bytes());
        buf.extend_from_slice("|".as_bytes());
        buf.extend_from_slice(self.message.as_bytes());
        buf.extend_from_slice("\0".as_bytes());
        buf.freeze()
    }

    pub fn deserialize(buf: &[u8]) -> Self {
        let mut user_id = String::new();
        let mut time_stamp = String::new();
        let mut message = String::new();

        let string = String::from_utf8_lossy(buf);

        let iter: Vec<&str> = string.split('|').collect();

        if iter.len() != 3 {
            error!("Invalid packet: {}", string);
            return Self {
                user_id: String::new(),
                time_stamp: String::new(),
                message: String::new(),
            };
        }

        user_id.push_str(iter[0]);
        time_stamp.push_str(iter[1]);
        message.push_str(iter[2]);

        Self {
            user_id,
            time_stamp,
            message,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_packet_serialize() {
        let packet = Packet {
            user_id: String::from("test"),
            time_stamp: String::from("2021-01-01 00:00:00"),
            message: String::from("Hello, world!"),
        };

        let buf = packet.serialize();

        assert_eq!(
            String::from_utf8_lossy(&buf),
            "test|2021-01-01 00:00:00|Hello, world!\0"
        );
    }

    #[test]
    fn test_packet_deserialize() {
        let buf = String::from("test|2021-01-01 00:00:00|Hello, world!").into_bytes();

        let packet = Packet::deserialize(&buf);

        assert_eq!(packet.user_id, "test");
        assert_eq!(packet.time_stamp, "2021-01-01 00:00:00");
        assert_eq!(packet.message, "Hello, world!");
    }

    #[test]
    fn test_packet_deserialize_invalid() {
        let buf = String::from("test|2021-01-01 00:00:00").into_bytes();

        let packet = Packet::deserialize(&buf);

        assert_eq!(packet.user_id, "");
        assert_eq!(packet.time_stamp, "");
        assert_eq!(packet.message, "");
    }

    #[test]
    fn test_packet_deserialize_empty() {
        let buf = String::from("").into_bytes();

        let packet = Packet::deserialize(&buf);

        assert_eq!(packet.user_id, "");
        assert_eq!(packet.time_stamp, "");
        assert_eq!(packet.message, "");
    }
}
