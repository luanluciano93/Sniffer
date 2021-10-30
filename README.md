# Sniffer
TibiaApi Packet Sniffer only for version 12.72 or higher. (Older protocols may not work as intended)

This program uses [TibiaApi](https://github.com/jo3bingham/TibiaAPI) made by [jo3bingham](https://github.com/jo3bingham), 

This is [my](https://github.com/marcosvf132/) fork of [AtefR](https://github.com/AtefR/Sniffer) repository, check his repository for more informations about the original version.

# How to use:

To use this program, first you need to edit your client.exe:

- Change it's RSA from original RSA to OpenTibia RSA.

- Change it's WebLoginService (Login IP) to http://127.0.0.1:7171/ or your custom port you want to use.

- If you don't know how to change this two things, use this excellent tutorial from [OtServ-BR](https://forums.otserv.com.br/index.php?/forums/topic/169530-cliente-tibia-1264-com-notepad/)

- Open this program, click on 'open' button, go to your client directory and select the file 'packages.json'. Notice that this file is not on 'bin' folder, but on the folder before it.

- Change the IP and port to the server you wan't to connect and click on 'connect'. There is another button on the side of it that shows the error log, if any.

- Once you click on it, it's already ready to login. Run your client.exe with the IP and RSA changed and you can login on your character. If you are trying to connect to the official server, only enter on Zuna/Zunera world.

# Tracker:

- To initialize the tracker, open my edited version of remeres, open or create a new world and click on 'start tracker' on the 'tracker' tab. Make sure that your world have the right dimensions to fit the tracked map, otherwhise it won't work. 

# Is it safe?

- This program can work on official servers but please consider not doing it. I don't take any responsability of any damage it can cause to you or your account.

# How to read:

The server, official or not, send us an array of bytes, the first 8 bytes on the array we don't need to worry about them. The 9th byte is the opCode, or header, that the client and the server use to identify what kind of packet we are dealing with. This header is the one you will search on your protocolgame.cpp or any other message reader. The next bytes you will need to have more knowledge about how to deal with them, but i will give a hint about how to translate bytes to a numeric value:
- 1 byte represents a numeric value that can go from 0 to 255. 
> **uint8_t value = msg.readByte();**


- 2 bytes represents a numeric value that can go from 0 to 65535.
> **uint16_t value = msg.read<uint16_t>();**


- 4 bytes represents a numeric value that can go from 0 to 4294967295.
> **uint32_t value = msg.read<uint32_t>();**


- 8 bytes represents a numeric value that can go from 0 to 18446744073709551615.
> **uint64_t value = msg.read<uint64_t>();**


- A string on the array is represented first by 2 bytes (uint16_t) that tell us the lenght of the string, and then the characters as 'char'.
> **std::string value = msg.readString();**


- A bool value is nothing more than a 1 byte (uint8) that is 'true' when the byte is '1' and 'false' when the byte is '0'.
> **bool value = msg.readBool();**

- You can use some hexadecimal editor to help you on converting a hex value to decimal. If you wan't a more easy tool then you can use [this](https://hexed.it/).

# Screenshot:
![5](https://user-images.githubusercontent.com/66353315/139513029-0d31c87b-94a1-43de-ab4b-bd6cc5e07abd.png)


