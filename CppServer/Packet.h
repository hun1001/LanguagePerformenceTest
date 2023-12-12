#pragma once

struct Packet
{
    std::string UserID;
    std::string TimeStamp;
    std::string Message;

    Packet(std::string userID, std::string timeStamp, std::string message)
    {
        UserID = userID;
        TimeStamp = timeStamp;
        Message = message;
    }

    BYTE* Serialize()
    {
        std::string packetData = UserID + "|" + TimeStamp + "|" + Message;

        BYTE* bin = new BYTE[packetData.length() + 1];
		memcpy(bin, packetData.c_str(), packetData.length() + 1);

        return bin;
    }

    static Packet Deserialize(BYTE* bin, int length)
    {
        std::string packetData = (char*)bin;

		std::string userID = "";
		std::string timeStamp = "";
		std::string message = "";

		int index = 0;
		while (packetData[index] != '|')
		{
			userID += packetData[index];
			index++;
		}
		index++;
		while (packetData[index] != '|')
		{
			timeStamp += packetData[index];
			index++;
		}
		index++;
		while (index < length)
		{
			message += packetData[index];
			index++;
		}

		return Packet(userID, timeStamp, message);
    }
};