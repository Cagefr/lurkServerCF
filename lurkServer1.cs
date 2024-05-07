using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;


// Used ChatGPT for some conversions, but I got everything to work except type 0 handeling. I am trying to use this project as a way of learning C#

public class MessagePacket {
    public byte type = 1;
    public ushort messageLength;
    public string recipientName;
    public string senderName;
    public bool isNarration;
    public string message;

    public byte[] Serialize() {
        byte[] data = new byte[65 + Encoding.UTF8.GetByteCount(message)];

        // Serialize type
        data[0] = type;

        // Serialize message length
        byte[] lengthBytes = BitConverter.GetBytes(messageLength);
        Array.Copy(lengthBytes, 0, data, 1, lengthBytes.Length);

        // Serialize recipient name
        byte[] recipientBytes = Encoding.UTF8.GetBytes(recipientName.PadRight(32, '\0'));
        Array.Copy(recipientBytes, 0, data, 3, recipientBytes.Length);

        // Serialize sender name
        byte[] senderBytes = Encoding.UTF8.GetBytes(senderName.PadRight(30, '\0'));
        Array.Copy(senderBytes, 0, data, 35, senderBytes.Length);

        // Serialize sender name end or narration marker
        if (isNarration) {
            data[65] = 0;
            data[66] = 1;
        } else {
            data[65 + senderBytes.Length] = 0;
            data[65 + senderBytes.Length + 1] = 0;
        }

        // Serialize message
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        Array.Copy(messageBytes, 0, data, 67, messageBytes.Length);

        return data;
    }

    public static MessagePacket Deserialize(byte[] data) {
        MessagePacket packet = new MessagePacket();

        // Deserialize type
        packet.type = data[0];

        // Deserialize message length
        packet.messageLength = BitConverter.ToUInt16(data, 1);

        // Deserialize recipient name
        packet.recipientName = Encoding.UTF8.GetString(data, 3, 32).TrimEnd('\0');

        // Deserialize sender name
        int senderNameEnd = Array.IndexOf(data, (byte)0, 35, 30);
        packet.senderName = Encoding.UTF8.GetString(data, 35, senderNameEnd - 35);

        // Determine if it's a narration
        packet.isNarration = (data[65] == 0 && data[66] == 1);

        // Deserialize message
        packet.message = Encoding.UTF8.GetString(data, 67, data.Length - 67);

        return packet;
    }
}

public class VersionPacket {
    public byte type = 14;
    public byte major;
    public byte minor;
    public ushort extension_length = 0;
    public VersionPacket(byte p_major, byte p_minor) {
        major = p_major;
        minor = p_minor;
    }
    public bool Send(Socket socket) {
        byte[] buffer = new byte[5];
        buffer[0] = type;
        buffer[1] = major;
        buffer[2] = minor;
        buffer[3] = (byte)(extension_length & 0xFF);
        buffer[4] = (byte)((extension_length >> 8) & 0xFF);
        return 5 == socket.Send(buffer);
    }
}

public class GamePacket {
    public byte type = 11;
    public ushort initial_points;
    public ushort stat_limit;
    public ushort description_length;
    public string description;
    public GamePacket(ushort ip, ushort sl, string d) {
        initial_points = ip;
        stat_limit = sl;
        description = d;
    }
    public bool Send(Socket socket) {
        byte[] descriptionBuffer = Encoding.UTF8.GetBytes(description);
        description_length = (ushort)descriptionBuffer.Length;
        byte[] buffer = new byte[7 + description_length];
        buffer[0] = type;
        buffer[1] = (byte)(initial_points & 0xFF);
        buffer[2] = (byte)((initial_points >> 8) & 0xFF);
        buffer[3] = (byte)(stat_limit & 0xFF);
        buffer[4] = (byte)((stat_limit >> 8) & 0xFF);
        buffer[5] = (byte)(description_length & 0xFF);
        buffer[6] = (byte)((description_length >> 8) & 0xFF);
        Array.Copy(descriptionBuffer, 0, buffer, 7, description_length);
        return buffer.Length == socket.Send(buffer);
    }
}


public class CharacterPacket {
    public byte type = 10;
    public string name;
    public byte flags;
    public ushort attack;
    public ushort defence;
    public ushort regen;
    public short health;
    public ushort gold;
    public ushort room_num;
    public ushort desc_length;
    public string desc;
    public Socket client;
}

public class RoomPacket {
    public byte type = 9;
    public ushort num;
    public string name;
    public ushort desc_length;
    public string desc;
    public RoomConnection[] connections; // New property to store connections

    public RoomPacket(ushort num, string name, string desc)
    {
        this.num = num;
        this.name = name;
        this.desc = desc;
        this.desc_length = (ushort)desc.Length;
    }

}

//This class stores room connections
public class RoomConnection
{
    public ushort RoomNumber { get; set; }
    public string RoomName { get; set; }
    public string Description { get; set; }

    public RoomConnection(ushort roomNumber, string roomName, string description)
    {
        RoomNumber = roomNumber;
        RoomName = roomName;
        Description = description;
    }
}


public class ConnectionPacket {
    public byte type = 13;
    public ushort roomNum;
    public string roomName;
    public ushort descriptionLength;
    public string roomDescription;

    public ConnectionPacket(ushort num, string name, string description) {
        roomNum = num;
        roomName = name;
        roomDescription = description;
        descriptionLength = (ushort)description.Length;
    }

    public byte[] Serialize() {
        byte[] nameBytes = Encoding.UTF8.GetBytes(roomName);
        byte[] descriptionBytes = Encoding.UTF8.GetBytes(roomDescription);

        byte[] packet = new byte[37 + descriptionBytes.Length];
        packet[0] = type;
        Buffer.BlockCopy(BitConverter.GetBytes(roomNum), 0, packet, 1, 2);
        Buffer.BlockCopy(nameBytes, 0, packet, 3, Math.Min(nameBytes.Length, 32));
        Buffer.BlockCopy(BitConverter.GetBytes(descriptionLength), 0, packet, 35, 2);
        Buffer.BlockCopy(descriptionBytes, 0, packet, 37, descriptionBytes.Length);

        return packet;
    }


}

public class MonsterPacket {
    public byte type = 10;
    public string name = "Default Monster";
    public byte flag = 0xff;
    public ushort attack = 100;
    public ushort defense = 28;
    public ushort regen = 33;
    public short health = 100;
    public ushort gold = 0;
    public ushort room_num = 9;
    public ushort dlen = 100;
    public string description = "This is a default goober";
}


public class AcceptPacket {
    public byte type = 8;
    public byte type_accepted;
}

public class RejectPacket {
    public byte type = 7;
    public byte error_code;
    public ushort err_msg_length;
    public string err_msg;
}

public class Server
{
    
    private static Socket listener;

    private static List<CharacterPacket> PlayerStruct = new List<CharacterPacket>();

    public static void Main(string[] args)
    {
        int listenPort = 5016;

        if (args.Length > 0 && !int.TryParse(args[0], out listenPort))
        {
            Console.WriteLine($"Invalid port: {args[0]}");
            return;
        }

        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, listenPort));
        listener.Listen(5);

        

        Console.WriteLine("*****    **      ****    *****  **   **");
        Console.WriteLine("**     ******    **  **  ***    ** * **");
        Console.WriteLine("**    **     **  **  **  **     **  ***");
        Console.WriteLine("***** **     **  ****    *****  **   **");

        Console.WriteLine($"Listening on port {listenPort}...");

        

        try
        {
            while (true)
            {
                Socket client = listener.Accept();
                IPEndPoint clientEndPoint = (IPEndPoint)client.RemoteEndPoint;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nConnection made from address {clientEndPoint.Address}");
                Console.ResetColor();
                Console.WriteLine("");

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }


    }


    private static void HandleClient(Socket client)
    {

        
        try
        {
            VersionPacket version = new VersionPacket(6, 17);
            version.Send(client);

            GamePacket game = new GamePacket(200, 65535, "An even better game than the last one! Future Edition");
            game.Send(client);


            //UNIVERSAL VARIABLE DECLARATIONS
            bool reject = true; //This one stays true for my loop
            bool start_Game = false;
            int rejected_packet = 0; //This one is different, I use this one to tell the server to send a reject packet
            bool room_activity = false; //Set if something needs to be done within a room
            ushort cur_Room = 0; //Not needed as far as I am in the project
            ushort nextRoom;
            bool changeRoomActive = false;
            bool fightActive = false;
            bool lootCall = false;
            bool playerDead = false;
            bool gotMessage = false;
            string lootTarget = null;
            //Room looting variables
            int lootRoom1 = 0;
            int lootRoom2 = 0;
            int lootRoom3 = 0;
            int lootRoom4 = 0;
            int lootRoom5 = 0;
            int lootRoom6 = 0;
            int lootRoom7 = 0;
            int lootRoom8 = 0;
            int lootRoom9 = 0;
            int lootRoom10 = 0;
            //END OF VARIABLE DECLARATIONS

            //Declare room packets
            RoomPacket room, room2, room3, room4, room5, room6, room7, room8, room9, room10;

            List<RoomPacket> rooms = new List<RoomPacket>();

            //When a room is created, it passed to the RoomPacket, this has 3 parameters the room number, name, and descriptions. I specifically implimented this for storing
            //Connections. I store the num, name, desc, and desc_length within room so it can be accessed later. 
            //Room one
            room = new RoomPacket(1, "Half Bathroom", "This is the initial room, Please be patient while we start the game! I promise it doesn't stink too bad.");
            room.desc_length = (ushort)room.desc.Length;

            //Room two
            room2 = new RoomPacket(2, "Library", "A quiet place filled with books and knowledge.");
            room2.desc_length = (ushort)room2.desc.Length;

            //Room three
            room3 = new RoomPacket(3, "Armory", "An arsenal of weapons and armor.");
            room3.desc_length = (ushort)room3.desc.Length;

            room4 = new RoomPacket(4, "Study Center", "A closed off room with many desks and white boards.");
            room4.desc_length = (ushort)room4.desc.Length;

            room5 = new RoomPacket(5, "Ceremony Area", "Lots of benches and an alter in the middle.");
            room5.desc_length = (ushort)room5.desc.Length;
            
            room6 = new RoomPacket(6, "Courtyard", "fairly empty lot oustide the castle, surrounded by trees.");
            room6.desc_length = (ushort)room6.desc.Length;

            room7 = new RoomPacket(7, "Master Bedroom", "Large room with fancy shelves, dressers, and art. Also, a giant bed.");
            room7.desc_length = (ushort)room7.desc.Length;

            room8 = new RoomPacket(8, "Loft", "Climbed a ladder to get here, and there's a door on the other side. Mostly bare with some dusty boxes.");
            room8.desc_length = (ushort)room8.desc.Length;

            room9 = new RoomPacket(9, "Dual Arena", "Seating on both sides, and lots of damage. It looks like many people don't make it out of here...");
            room9.desc_length = (ushort)room9.desc.Length;

            room10 = new RoomPacket(10, "Holding Cell", "Very dark. You can hear water dripping, and see an open cell door in the back.");
            room10.desc_length = (ushort)room10.desc.Length;

            rooms.Add(room); //Half bathroom
            rooms.Add(room2); //Library
            rooms.Add(room3); //Armory
            rooms.Add(room4); //Study Center
            rooms.Add(room5); //Ceremony Area
            rooms.Add(room6); //Courtyard
            rooms.Add(room7); //Master Bedroom
            rooms.Add(room8); //Loft
            rooms.Add(room9); //Dual Arena
            rooms.Add(room10); //Holding Cell

            //Room connections
            // Define connections for room 1 connected to rooms 2 and 3
            //Half Bathroom
            RoomConnection[] roomConnections1 = new RoomConnection[] //If wanting to work on other room connections roomConnections2 = new RoomConnection[]
            {
                new RoomConnection(2, "Library", "A quiet place filled with books and knowledge."),
                new RoomConnection(3, "Armory", "An arsenal of weapons and armor.")
            };

            //Library connections room 2
            RoomConnection[] roomConnections2 = new RoomConnection[] 
            {
                new RoomConnection(1, "Half Bathroom", "One toilet, hopefully it doesn't stink"),
                new RoomConnection(4, "Study Center", "A closed off room with many desks and white boards."),
                new RoomConnection(7, "Master Bedroom", "Large room with fancy shelves, dressers, and art. Also, a giant bed."),
            };

            //Armory connections room 3
            RoomConnection[] roomConnections3 = new RoomConnection[]
            {
                new RoomConnection(1, "Half Bathroom", "One toilet, hopefully it doesn't stink"),
                new RoomConnection(5, "Ceremony Area", "Lots of benches and an alter in the middle."),
                new RoomConnection(6, "Courtyard", "fairly empty lot oustide the castle, surrounded by trees."),
            };

            //Study Center connections room 4
            RoomConnection[] roomConnections4 = new RoomConnection[]
            {
                new RoomConnection(2, "Library", "A quiet place filled with books and knowledge."),
                
            };

            //Ceremony Area connections room 5
            RoomConnection[] roomConnections5 = new RoomConnection[]
            {
                new RoomConnection(3, "Armory", "An arsenal of weapons and armor."),
                new RoomConnection(6, "Courtyard", "fairly empty lot oustide the castle, surrounded by trees."),
            };

            //Courtyard connections room 6
            RoomConnection[] roomConnections6 = new RoomConnection[]
            {
                new RoomConnection(3, "Armory", "An arsenal of weapons and armor."),
                new RoomConnection(5, "Ceremony Area", "Lots of benches and an alter in the middle."),
                new RoomConnection(8, "Loft", "Climbed a ladder to get here, and there's a door on the other side. Mostly bare with some dusty boxes."),
                new RoomConnection(10, "Holding Cell", "Very dark. You can hear water dripping, and see an open cell door in the back."),
            };

            //Master Bedroom connections room 7
            RoomConnection[] roomConnections7 = new RoomConnection[]
            {
                new RoomConnection(2, "Library", "A quiet place filled with books and knowledge."),
                new RoomConnection(9, "Dual Arena", "Seating on both sides, and lots of damage. It looks like many people don't make it out of here..."),
                
            };

            //Loft connections room 8
            RoomConnection[] roomConnections8 = new RoomConnection[]
            {
                new RoomConnection(6, "Courtyard", "fairly empty lot oustide the castle, surrounded by trees."),
                new RoomConnection(9, "Dual Arena", "Seating on both sides, and lots of damage. It looks like many people don't make it out of here..."),
            };

            //Dual Arena room 9
            RoomConnection[] roomConnections9 = new RoomConnection[]
            {
                new RoomConnection(7, "Master Bedroom", "Large room with fancy shelves, dressers, and art. Also, a giant bed."),
                new RoomConnection(8, "Loft", "Climbed a ladder to get here, and there's a door on the other side. Mostly bare with some dusty boxes."),
            };

            //Holding Cell room 10
            RoomConnection[] roomConnections10 = new RoomConnection[]
            {
                new RoomConnection(6, "Courtyard", "fairly empty lot oustide the castle, surrounded by trees."),
            };

            RoomPacket room1Pack = new RoomPacket(1, "Half Bathroom", "This is the initial room, Please be patient while we start the game! I promise it doesn't stink too bad.");
            room1Pack.connections = roomConnections1;
            RoomPacket room2Pack = new RoomPacket(2, "Library", "A quiet place filled with books and knowledge.");
            room2Pack.connections = roomConnections2;
            RoomPacket room3Pack = new RoomPacket(3, "Armory", "An arsenal of weapons and armor.");
            room3Pack.connections = roomConnections3;
            RoomPacket room4Pack = new RoomPacket(4, "Study Center", "A closed off room with many desks and white boards.");
            room4Pack.connections = roomConnections4;
            RoomPacket room5Pack = new RoomPacket(5, "Ceremony Area", "Lots of benches and an alter in the middle.");
            room5Pack.connections = roomConnections5;
            RoomPacket room6Pack = new RoomPacket(6, "Courtyard", "fairly empty lot oustide the castle, surrounded by trees.");
            room6Pack.connections = roomConnections6;
            RoomPacket room7Pack = new RoomPacket(7, "Master Bedroom", "Large room with fancy shelves, dressers, and art. Also, a giant bed.");
            room7Pack.connections = roomConnections7;
            RoomPacket room8Pack = new RoomPacket(8, "Loft", "Climbed a ladder to get here, and there's a door on the other side. Mostly bare with some dusty boxes.");
            room8Pack.connections = roomConnections8;
            RoomPacket room9Pack = new RoomPacket(9, "Dual Arena", "Seating on both sides, and lots of damage. It looks like many people don't make it out of here...");
            room9Pack.connections = roomConnections9;
            RoomPacket room10Pack = new RoomPacket(10, "Holding Cell", "Very dark. You can hear water dripping, and see an open cell door in the back.");
            room10Pack.connections = roomConnections10;

            //Define clients connected

            //DEFINING MONSTERS HERE
            //Goint to attempt to define monsters here

            MonsterPacket goobyA = new MonsterPacket();
            MonsterPacket doobyA = new MonsterPacket();
            MonsterPacket zoobyA = new MonsterPacket();
            MonsterPacket groovyA = new MonsterPacket();
            MonsterPacket shoobyBatA = new MonsterPacket();
            MonsterPacket froobyA = new MonsterPacket();


            // Setting properties

            //GOOBY MONSTER
            goobyA.name = "Gooby";
            goobyA.flag = 0xff;
            goobyA.attack = 10;
            goobyA.defense = 10;
            goobyA.regen = 10;
            goobyA.health = 10;
            goobyA.gold = 0;
            goobyA.room_num = 3; //Bro is in room 3
            goobyA.dlen = 38;
            goobyA.description = "Small cute blob, can be lethal though.";
            bool goobyA_Dead = false;

            //DOOBY Monster
            doobyA.name = "Dooby";
            doobyA.flag = 0xff;
            doobyA.attack = 20;
            doobyA.defense = 20;
            doobyA.regen = 20;
            doobyA.health = 20;
            doobyA.gold = 0;
            doobyA.room_num = 5; //Bro is in room 5
            doobyA.dlen = 44;
            doobyA.description = "Like a gooby, but it's been hitting the gym.";
            bool doobyA_Dead = false;

            //Zooby
            zoobyA.name = "Zooby";
            zoobyA.flag = 0xff;
            zoobyA.attack = 5;
            zoobyA.defense = 70;
            zoobyA.regen = 20;
            zoobyA.health = 65;
            zoobyA.gold = 0;
            zoobyA.room_num = 4; //bro is in room 4
            zoobyA.dlen = 66;
            zoobyA.description = "A slimeball with a crazy amount of health, doesn't look to deadly.";
            bool zoobyA_Dead = false;

            //Groovy
            groovyA.name = "Groovy";
            groovyA.flag = 0xff;
            groovyA.attack = 40;
            groovyA.defense = 50;
            groovyA.regen = 10;
            groovyA.health = 50;
            groovyA.gold = 0;
            groovyA.room_num = 6; //bro is in room 6
            groovyA.dlen = 89;
            groovyA.description = "This slime ball knows how to dance, this could pose a problem. Hard to see its next move.";
            bool groovyA_Dead = false;

            //Shoobybat
            shoobyBatA.name = "ShoobyBat";
            shoobyBatA.flag = 0xff;
            shoobyBatA.attack = 30;
            shoobyBatA.defense = 30;
            shoobyBatA.regen = 10;
            shoobyBatA.health = 100;
            shoobyBatA.gold = 0;
            shoobyBatA.room_num = 7; //bro is in room 7
            shoobyBatA.dlen = 48;
            shoobyBatA.description = "Slime ball with bat wings, it's kind of big too!";
            bool shoobyBatA_Dead = false;

            froobyA.name = "Frooby";
            froobyA.flag = 0xff;
            froobyA.attack = 200;
            froobyA.defense = 200;
            froobyA.regen = 3;
            froobyA.health = 200;
            froobyA.gold = 0;
            froobyA.room_num = 10; //bro is in room 7
            froobyA.dlen = 60;
            froobyA.description = "What a horrendous creature, we can't beat this demon easily!";
            bool froobyA_Dead = false;



            CharacterPacket character = null; // Declare character outside the if statement
            //***************************************
            //THIS LOOP ESSENTIALLY STARTS EVERYTHING
            //***************************************
            while (reject)
            {
                byte[] characterBeginning = new byte[67];
                int bytesRead = client.Receive(characterBeginning, 0, 67, SocketFlags.None); //NEEDED THIS LINE, TOOK it out and it broke
                //Had this at 48

            
                //int roomChange = 0;
                //bool changingRoom = false;
                
                int Type = characterBeginning[0];


                Console.WriteLine(Type); //Show what command was trying to be used

                if (Type == 10)
                {
                    /*
                    byte[] readCharacter = new byte[46];
                    client.Receive(readCharacter, 0, 46, SocketFlags.None);
                    */

                
                    character = new CharacterPacket();
                    character.name = Encoding.UTF8.GetString(characterBeginning, 1, 32);
                    character.flags = characterBeginning[33]; //0x98 for normal flag
                    character.attack = BitConverter.ToUInt16(characterBeginning, 34);
                    character.defence = BitConverter.ToUInt16(characterBeginning, 36);
                    character.regen = BitConverter.ToUInt16(characterBeginning, 38);
                    character.health = 100; //Default Health
                    character.gold = 0; //Default Gold
                    character.room_num = 1; //Default Room number
                    character.client = client;
                    character.desc_length = BitConverter.ToUInt16(characterBeginning, 46);

                    /*
                    character = new CharacterPacket();
                    character.name = Encoding.UTF8.GetString(readCharacter, 1, 32);
                    character.flags = readCharacter[33]; //0x98 for normal flag
                    character.attack = BitConverter.ToUInt16(readCharacter, 34);
                    character.defence = BitConverter.ToUInt16(readCharacter, 36);
                    character.regen = BitConverter.ToUInt16(readCharacter, 38);
                    character.health = 100; //Default Health
                    character.gold = 0; //Default Gold
                    character.room_num = 1; //Default Room number
                    character.client = client;
                    character.desc_length = BitConverter.ToUInt16(readCharacter, 46);
                    */

                    byte[] descBuffer = new byte[character.desc_length];
                    client.Receive(descBuffer, 0, character.desc_length, SocketFlags.None);
                    character.desc = Encoding.UTF8.GetString(descBuffer);

                    ushort stats = (ushort)(character.attack + character.defence + character.regen);

                    character.flags = 0x98; //0x98 for normal flag

                    if (stats <= 200 ) // Defining max
                    {
                        AcceptPacket accept = new AcceptPacket();
                        accept.type_accepted = 10;
                        client.Send(new byte[] { accept.type, accept.type_accepted });

                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to decode it correctly
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);

                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Store Client stats
                        
                    }
                    else{
                        rejected_packet = 1; //Set to 1 for rejected stats
                    }

                    //Print out our character in the server terminal
                    Console.WriteLine($"Name: {character.name}");
                    Console.WriteLine($"Flags: {character.flags}");
                    Console.WriteLine($"Join Battle: {(character.flags & 0x40) != 0}");
                    Console.WriteLine($"Monster: {(character.flags & 0x20) != 0}");
                    Console.WriteLine($"Started: {(character.flags & 0x10) != 0}");
                    Console.WriteLine($"Ready: {(character.flags & 0x08) != 0}");
                    Console.WriteLine($"Alive: {(character.flags & 0x80) != 0}");
                    Console.WriteLine($"sizeof(struct flag_byte) = 1"); //0xff is an example of a flag
                    Console.WriteLine($"Alive: {(character.flags & 0x80) != 0}");
                    Console.WriteLine($"Join Battle: {(character.flags & 0x40) != 0}");
                    Console.WriteLine($"Monster: {(character.flags & 0x20) != 0}");
                    Console.WriteLine($"Started: {(character.flags & 0x10) != 0}");
                    Console.WriteLine($"Ready: {(character.flags & 0x08) != 0}");
                    Console.WriteLine($"Name: {character.name}");
                    Console.WriteLine($"Attack: {character.attack}");
                    Console.WriteLine($"Defence: {character.defence}");
                    Console.WriteLine($"Regen: {character.regen}");
                    Console.WriteLine($"Health: {character.health}");
                    Console.WriteLine($"Gold: {character.gold}");
                    Console.WriteLine($"Current Room: {character.room_num}");
                    Console.WriteLine($"Description Length: {character.desc_length}");
                    Console.WriteLine($"Description: {character.desc}");

                    character.client = client;

                    cur_Room = character.room_num; //Stores the variable of the current room

                    PlayerStruct.Add(character); //Add character to list of characters in game

                }
                else if (Type == 6) // START GAME
                {
                    //byte[] startbuffer = new byte [1];
                    Console.WriteLine("Got Inside (START) if statement");
                    start_Game = true;

                    AcceptPacket accept = new AcceptPacket();
                    accept.type_accepted = 10; //Might not be the correct thing to send
                    client.Send(new byte[] { accept.type, accept.type_accepted });


                    //Send Character
                    byte[] charBuffer = new byte[48];
                    charBuffer[0] = 10; //starts with 10 to read it in correctly.
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                    charBuffer[33] = character.flags;
                    Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                    client.Send(charBuffer);
                    client.Send(Encoding.UTF8.GetBytes(character.desc));
                    //THIS IS BEING SENT TWICE, MAY BE UNEEDED WITHIN START
                    //Send room that character is in

                    //Was sending a room buffer her, but this gets sent when room_activity gets set to true

                    //Was sending a character here, but uneeded because it gets sent in room


                    room_activity = true;
                }
                else if (Type == 2) //CHANGEROOM
                {  
                    if (start_Game == true)
                    {
                        cur_Room = character.room_num; //Set room before room change

                        ushort roomNumberToChangeTo = BitConverter.ToUInt16(characterBeginning, 1); //CharacterBeginning is the bytes being read in
                        ushort room_Change = roomNumberToChangeTo;
                        nextRoom = roomNumberToChangeTo;
                        //character.room_num = room_Change; //Sets the new room of the character
                        //room_activity = true;
                        changeRoomActive = true;

                        //CHECK FOR BAD ROOM CHANGE
                        if ((cur_Room == 1) && ((nextRoom != 2) && (nextRoom != 3))) //Checks for room 1
                        {
                            Console.WriteLine("Improper room attempt from room 1");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if ((cur_Room == 2) && ((nextRoom != 1) && (nextRoom != 4) && (nextRoom != 7))) //Checks for room 2
                        {
                            Console.WriteLine("Improper room attempt from room 2");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if ((cur_Room == 3) && ((nextRoom != 1) && (nextRoom != 5) && (nextRoom != 6))) //Checks for room 3
                        {
                            Console.WriteLine("Improper room attempt from room 3");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if((cur_Room == 4) && (nextRoom != 2)) //Checks for room 4
                        {
                            Console.WriteLine("Improper room attempt from room 4");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if ((cur_Room == 5) && ((nextRoom != 3) && (nextRoom != 6))) //Checks for room 5
                        {
                            Console.WriteLine("Improper room attempt from room 5");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if ((cur_Room == 6) && ((nextRoom != 3) && (nextRoom != 5) && (nextRoom != 8) && (nextRoom != 10))) //Checks for room 6
                        {
                            Console.WriteLine("Improper room attempt from room 6");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if ((cur_Room == 7) && ((nextRoom != 2) && (nextRoom != 9))) //Checks for room 7
                        {
                            Console.WriteLine("Improper room attempt from room 7");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if ((cur_Room == 8) && ((nextRoom != 6) && (nextRoom != 9)))
                        {
                            Console.WriteLine("Improper room attempt from room 8");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if ((cur_Room == 9) && ((nextRoom != 7) && (nextRoom != 8)))
                        {
                            Console.WriteLine("Improper room attempt from room 9");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else if ((cur_Room == 10) && (nextRoom != 6))
                        {
                            Console.WriteLine("Improper room attempt from room 10");
                            rejected_packet = 4; //Bad room error activated
                        }
                        else //Checked for all, and must be a good room change
                        {
                            room_activity = true;
                            character.room_num = room_Change;
                        }

                    }
                    else
                    {
                        rejected_packet = 3; //Sends that game hasn't started
                    }
                   
                }
                else if (Type == 1)
                {
                    //Lets impliment message here
                    MessagePacket Message = MessagePacket.Deserialize(characterBeginning);

                    // Extract data from the message
                    string recipientName = Message.recipientName;
                    string senderName = Message.senderName;
                    string messageContent = Message.message;
                    ushort messageLength = (ushort)messageContent.Length;

                    byte[] mesDescBuffer = new byte[messageLength];
                    client.Receive(mesDescBuffer, 0, messageLength, SocketFlags.None);
                    messageContent = Encoding.UTF8.GetString(mesDescBuffer);

                    gotMessage = true;
                    // Process the extracted data
                    Console.WriteLine("Received message:");
                    Console.WriteLine($"Recipient: {recipientName}");
                    Console.WriteLine($"Sender: {senderName}");
                    Console.WriteLine($"Message Length: {Message.messageLength}");
                    Console.WriteLine($"Message: {messageContent}");

                    // Prepare the message byte array
                    byte[] data = new byte[67 + messageLength];
                    data[0] = 1; // Type
                    byte[] lengthBytes = BitConverter.GetBytes(messageLength);
                    Array.Copy(lengthBytes, 0, data, 1, 2);
                    Encoding.UTF8.GetBytes(recipientName).CopyTo(data, 3);
                    Encoding.UTF8.GetBytes(senderName).CopyTo(data, 35);
                    Encoding.UTF8.GetBytes(messageContent).CopyTo(data, 67);

                   //This works
                    foreach (var dude in PlayerStruct){
                        dude.client.Send(data);
                    }
                
                }
                else if (Type == 12)
                {
                    Console.WriteLine("Player requested to leave the game.");
                    break; //This break statement should disconnect the client
                }
                else if (Type == 3) //FIGHT
                {
                    if(start_Game == true){
                        //This is where fight will be implimented
                        room_activity = true;
                        fightActive = true;
                        Console.WriteLine("Fight was called");
                    }
                    else
                    {
                        rejected_packet = 3; //Sends that game hasn't started
                        Console.WriteLine("Game hasn't started when FIGHT called");
                    }
                }
                else if (Type == 4) //PVP FIGHT
                {
                    if(start_Game == true)
                    {
                        rejected_packet = 5; //Error for no pvp
                    }
                    else
                    {
                        rejected_packet = 3; //Sends that game hasn't started
                        Console.WriteLine("Game hasn't started when PVP called");
                    }
                }
                else if (Type == 5) //LOOT
                {
                    if (start_Game == true)
                    {
                        lootTarget = Encoding.UTF8.GetString(characterBeginning, 1, 32);
                        lootCall = true;
                        Console.WriteLine($"{character.name} initiated loot");
                    }
                    else
                    {
                        rejected_packet = 3; //Sends that game hasn't started
                        Console.WriteLine("Game hasn't started when LOOT called");
                    }
                }
                else
                {
                    rejected_packet = 2; //Set to 2 for unexpected packet recieved
                }

                

                //Handle Room connections
                // Send room connections
                // Check for room activity, if so then activate
                if (room_activity == true)
                {
                    //************************************//
                    //*           Room 1                 *//
                    //************************************//
                    if (1 == character.room_num) //This means if current charcter room is 1
                    {
                        if (lootCall == true)
                        {
                            lootRoom1 = lootRoom1 + 1;

                            if (lootRoom1 ==  1)
                            {
                                ushort prevGold = character.gold;
                                ushort goldGained = 12;
                                ushort newGold = (ushort)(prevGold + goldGained);
                                character.gold = newGold;
                                Console.WriteLine($"10 gold was looted by {character.name} in room 1");
                            }
                            else
                            {
                                Console.WriteLine($"{character.name} attempted to loot twice in room 1");
                                rejected_packet = 11;
                            }
                        }
                        
                        Console.WriteLine("Activity in room one - Half Bathroom");
                        //Send Room 1
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room.name), 0, roomBuffer, 3, room.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Player connections for room 1
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 1)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 1.");
                                }
                            }
                        }

                        //Room connections
                        foreach (RoomConnection conn in room1Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }

                        fightActive = false; // Turn off fight if it was called
                        //room_activity = false;
                        lootCall = false;
                    }
                    //************************************//
                    //*           Room 2                 *//
                    //************************************//
                    else if (2 == character.room_num) //This means if current charcter room is 2 //Aka the library
                    {
                        Console.WriteLine("Activity in room Two - Library");
                        //Send Room 2
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room2.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room2.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room2.name), 0, roomBuffer, 3, room2.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room2.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room2.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Player connections in room 2
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 2)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 2.");
                                }
                            }
                        }
                        
                        //Room connections
                        foreach (RoomConnection conn in room2Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }
                        
                        fightActive = false;
                        lootCall = false; // Turn off fight if it was called

                    }
                    //************************************//
                    //*           Room 3                 *//
                    //************************************//
                    else if (3 == character.room_num) // If player is in room 3 Armory
                    {
                        if (fightActive == true)
                        {
                            int playerDamage = Math.Max(0, character.attack - goobyA.defense);
                            int monsterDamage = Math.Max(0, goobyA.attack - character.defence);

                            goobyA.health -= (short)playerDamage;
                            character.health -= (short)monsterDamage;

                            goobyA.health += (short)(goobyA.regen * 10/100);
                            character.health += (short)(character.regen + 10/100);

                            if(goobyA.health <= 0){
                                Console.WriteLine("Gooby monster has died.");
                                //Make sure to change flags to show dead monster
                                goobyA.flag = 0x78; //this should indicate the monster is dead.
                                //Player death 0x00 if no join battle
                                //Player death 0x40 if join battle
                                goobyA_Dead = true;
                            }

                            if (character.health <= 0)
                            {
                                //This player should die
                                Console.WriteLine($"{character.name} was killed in room 3");
                                character.flags = 0x00;
                                playerDead = true;
                            
                            }
                        }

                        if (lootCall == true && goobyA_Dead == true)
                        {
                            lootRoom3 = lootRoom3 + 1;

                            Console.WriteLine($"Attempted target was {lootTarget}");

                            if (lootRoom3 ==  1)
                            {
                                ushort prevGold = character.gold;
                                ushort goldGained = 10;
                                ushort newGold = (ushort)(prevGold + goldGained);
                                character.gold = newGold;
                                Console.WriteLine($"10 gold was looted by {character.name} in room 3");
                            }
                            else
                            {
                                Console.WriteLine($"{character.name} attempted to loot twice in room 3");
                                rejected_packet = 11;
                            }
                        }

                        Console.WriteLine("Activity in room Three - Armory");
                        //Send room 3
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room3.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room3.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room3.name), 0, roomBuffer, 3, room3.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room3.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room3.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //SENDING MONSTER(s) IN ROOM
                        byte[] monsterBuffer = new byte[48];
                        monsterBuffer[0] = goobyA.type; // Use the actual type value of the monster packet
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(goobyA.name), 0, monsterBuffer, 1, goobyA.name.Length);
                        monsterBuffer[33] = goobyA.flag;
                        Buffer.BlockCopy(BitConverter.GetBytes(goobyA.attack), 0, monsterBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(goobyA.defense), 0, monsterBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(goobyA.regen), 0, monsterBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(goobyA.health), 0, monsterBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(goobyA.gold), 0, monsterBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(goobyA.room_num), 0, monsterBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(goobyA.dlen), 0, monsterBuffer, 46, 2);
                        client.Send(monsterBuffer);
                        client.Send(Encoding.UTF8.GetBytes(goobyA.description));

                        //Player connections in room 3
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 3)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 3.");
                                }
                            }
                        }

                        //Room 3 connections
                        foreach (RoomConnection conn in room3Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }

                        lootCall = false;
                        fightActive = false; // Turn off fight if it was called

                    }
                    //************************************//
                    //*           Room 4                 *//
                    //************************************//
                    else if (4 == character.room_num) //Room 4 Study room
                    {
                        if (fightActive == true)
                        {
                            int playerDamage = Math.Max(0, character.attack - zoobyA.defense);
                            int monsterDamage = Math.Max(0, zoobyA.attack - character.defence);

                            zoobyA.health -= (short)playerDamage;
                            character.health -= (short)monsterDamage;

                            zoobyA.health += (short)(zoobyA.regen * 10 / 100);
                            character.health += (short)(character.regen + 10 / 100);

                            if(zoobyA.health <= 0){
                                Console.WriteLine("Zooby monster has died. ROOM 4");
                                //Make sure to change flags to show dead monster
                                zoobyA.flag = 0x78; //this should indicate the monster is dead.
                                
                                zoobyA_Dead = true;
                            }

                            if (character.health <= 0)
                            {
                                //This player should die
                                Console.WriteLine($"{character.name} was killed in room 4");
                                character.flags = 0x00;
                                playerDead = true;
                            }
                        }

                        if (lootCall == true && zoobyA_Dead == true)
                        {
                            lootRoom4 = lootRoom4 + 1;

                            if (lootRoom4 ==  1)
                            {
                                ushort prevGold = character.gold;
                                ushort goldGained = 20;
                                ushort newGold = (ushort)(prevGold + goldGained);
                                character.gold = newGold;
                                Console.WriteLine($"10 gold was looted by {character.name} in room 4");
                            }
                            else
                            {
                                Console.WriteLine($"{character.name} attempted to loot twice in room 4");
                                rejected_packet = 11;
                            }
                        }

                        Console.WriteLine("Activity in room Four - Study Center");
                        //Send room 4
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room4.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room4.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room4.name), 0, roomBuffer, 3, room4.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room4.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room4.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Send Monster(s) in room 4
                        byte[] monsterBuffer = new byte[48];
                        monsterBuffer[0] = zoobyA.type; // Use the actual type value of the monster packet
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(zoobyA.name), 0, monsterBuffer, 1, zoobyA.name.Length);
                        monsterBuffer[33] = zoobyA.flag;
                        Buffer.BlockCopy(BitConverter.GetBytes(zoobyA.attack), 0, monsterBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(zoobyA.defense), 0, monsterBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(zoobyA.regen), 0, monsterBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(zoobyA.health), 0, monsterBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(zoobyA.gold), 0, monsterBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(zoobyA.room_num), 0, monsterBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(zoobyA.dlen), 0, monsterBuffer, 46, 2);
                        client.Send(monsterBuffer);
                        client.Send(Encoding.UTF8.GetBytes(zoobyA.description));

                        //Player connections in room 4
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 4)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 4.");
                                }
                            }
                        }

                        //Room 4 connections
                        foreach (RoomConnection conn in room4Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }

                        lootCall = false;
                        fightActive = false; // Turn off fight if it was called

                    }
                    //************************************//
                    //*           Room 5                 *//
                    //************************************//
                    else if(5 == character.room_num) //If we are in room 5 Ceremony Area
                    {
                        if (fightActive == true)
                        {
                            int playerDamage = Math.Max(0, character.attack - doobyA.defense);
                            int monsterDamage = Math.Max(0, doobyA.attack - character.defence);

                            doobyA.health -= (short)playerDamage;
                            character.health -= (short)monsterDamage;

                            doobyA.health += (short)(doobyA.regen * 10/100);
                            character.health += (short)(character.regen + 10/100);

                            if(doobyA.health <= 0){
                                Console.WriteLine("Dooby monster has died. ROOM 5");
                                //Make sure to change flags to show dead monster
                                doobyA.flag = 0x78; //this should indicate the monster is dead.
                            }

                            if (character.health <= 0)
                            {
                                //This player should die
                                Console.WriteLine($"{character.name} was killed in room 5");
                                character.flags = 0x00;
                                playerDead = true;
                            }
                        }

                        if (lootCall == true && doobyA_Dead == true)
                        {
                            lootRoom5 = lootRoom5 + 1;

                            if (lootRoom5 ==  1)
                            {
                                ushort prevGold = character.gold;
                                ushort goldGained = 30;
                                ushort newGold = (ushort)(prevGold + goldGained);
                                character.gold = newGold;
                                Console.WriteLine($"30 gold was looted by {character.name} in room 5");
                            }
                            else
                            {
                                Console.WriteLine($"{character.name} attempted to loot twice in room 5");
                                rejected_packet = 11;
                            }
                        }

                        Console.WriteLine("Activity in room Five - Ceremony Area");
                        //Send room 5
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room5.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room5.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room5.name), 0, roomBuffer, 3, room5.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room5.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room5.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Sending MONSTER(S) in room 5
                        byte[] monsterBuffer = new byte[48];
                        monsterBuffer[0] = doobyA.type; // Use the actual type value of the monster packet
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(doobyA.name), 0, monsterBuffer, 1, doobyA.name.Length);
                        monsterBuffer[33] = doobyA.flag;
                        Buffer.BlockCopy(BitConverter.GetBytes(doobyA.attack), 0, monsterBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(doobyA.defense), 0, monsterBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(doobyA.regen), 0, monsterBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(doobyA.health), 0, monsterBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(doobyA.gold), 0, monsterBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(doobyA.room_num), 0, monsterBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(doobyA.dlen), 0, monsterBuffer, 46, 2);
                        client.Send(monsterBuffer);
                        client.Send(Encoding.UTF8.GetBytes(doobyA.description));

                        //Player connections in room 5
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 5)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 5.");
                                }
                            }
                        }

                        //Room 5 connections
                        foreach (RoomConnection conn in room5Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }

                        lootCall = false;
                        fightActive = false; // Turn off fight if it was called

                    }
                    //************************************//
                    //*           Room 6                 *//
                    //************************************//
                    else if(6 == character.room_num) //If we are in room 6 Courtyard
                    {
                        if (fightActive == true)
                        {
                            int playerDamage = Math.Max(0, character.attack - groovyA.defense);
                            int monsterDamage = Math.Max(0, groovyA.attack - character.defence);

                            groovyA.health -= (short)playerDamage;
                            character.health -= (short)monsterDamage;

                            groovyA.health += (short)(groovyA.regen * 10 / 100);
                            character.health += (short)(character.regen + 10 / 100);

                            if(groovyA.health <= 0){
                                Console.WriteLine("Groovy monster has died. ROOM 6");
                                //Make sure to change flags to show dead monster
                                groovyA.flag = 0x78; //this should indicate the monster is dead.
                            }

                            if (character.health <= 0)
                            {
                                //This player should die
                                Console.WriteLine($"{character.name} was killed in room 6");
                                character.flags = 0x00;
                                playerDead = true;
                            }
                        }

                        if (lootCall == true && groovyA_Dead == true)
                        {
                            lootRoom6 = lootRoom6 + 1;

                            if (lootRoom6 ==  1)
                            {
                                ushort prevGold = character.gold;
                                ushort goldGained = 30;
                                ushort newGold = (ushort)(prevGold + goldGained);
                                character.gold = newGold;
                                Console.WriteLine($"30 gold was looted by {character.name} in room 6");
                            }
                            else
                            {
                                Console.WriteLine($"{character.name} attempted to loot twice in room 6");
                                rejected_packet = 11;
                            }
                        }

                        Console.WriteLine("Activity in room Six - Courtyard");
                        //Send room 6
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room6.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room6.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room6.name), 0, roomBuffer, 3, room6.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room6.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room6.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Sending MONSTER(S) in room 6
                        byte[] monsterBuffer = new byte[48];
                        monsterBuffer[0] = groovyA.type; // Use the actual type value of the monster packet
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(groovyA.name), 0, monsterBuffer, 1, groovyA.name.Length);
                        monsterBuffer[33] = groovyA.flag;
                        Buffer.BlockCopy(BitConverter.GetBytes(groovyA.attack), 0, monsterBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(groovyA.defense), 0, monsterBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(groovyA.regen), 0, monsterBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(groovyA.health), 0, monsterBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(groovyA.gold), 0, monsterBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(groovyA.room_num), 0, monsterBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(groovyA.dlen), 0, monsterBuffer, 46, 2);
                        client.Send(monsterBuffer);
                        client.Send(Encoding.UTF8.GetBytes(groovyA.description));

                        //Player connections in room 6
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 6)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 6.");
                                }
                            }
                        }

                        //Room 6 connections
                        foreach (RoomConnection conn in room6Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }

                        lootCall = false;
                        fightActive = false; // Turn off fight if it was called

                    }
                    //************************************//
                    //*           Room 7                 *//
                    //************************************//
                    else if(7 == character.room_num) //If we are in room 7 Master Bedroom
                    {
                        if (fightActive == true)
                        {
                            int playerDamage = Math.Max(0, character.attack - shoobyBatA.defense);
                            int monsterDamage = Math.Max(0, shoobyBatA.attack - character.defence);

                            shoobyBatA.health -= (short)playerDamage;
                            character.health -= (short)monsterDamage;

                            shoobyBatA.health += (short)(shoobyBatA.regen * 10 / 100);
                            character.health += (short)(character.regen + 10 / 100);

                            if(shoobyBatA.health <= 0){
                                Console.WriteLine("ShoobyBat monster has died. ROOM 7");
                                //Make sure to change flags to show dead monster
                                shoobyBatA.flag = 0x78; //this should indicate the monster is dead.
                            }

                            if (character.health <= 0)
                            {
                                //This player should die
                                Console.WriteLine($"{character.name} was killed in room 7");
                                character.flags = 0x00;
                                playerDead = true;
                            }
                        }

                        if (lootCall == true && shoobyBatA_Dead == true)
                        {
                            lootRoom7 = lootRoom7 + 1;

                            if (lootRoom7 ==  1)
                            {
                                ushort prevGold = character.gold;
                                ushort goldGained = 40;
                                ushort newGold = (ushort)(prevGold + goldGained);
                                character.gold = newGold;
                                Console.WriteLine($"30 gold was looted by {character.name} in room 7");
                            }
                            else
                            {
                                Console.WriteLine($"{character.name} attempted to loot twice in room 7");
                                rejected_packet = 11;
                            }
                        }

                        Console.WriteLine("Activity in room Seven - Master Bedroom");
                        //Send room 7
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room7.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room7.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room7.name), 0, roomBuffer, 3, room7.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room7.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room7.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Sending Monsters in room 7
                        byte[] monsterBuffer = new byte[48];
                        monsterBuffer[0] = shoobyBatA.type; // Use the actual type value of the monster packet
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(shoobyBatA.name), 0, monsterBuffer, 1, shoobyBatA.name.Length);
                        monsterBuffer[33] = shoobyBatA.flag;
                        Buffer.BlockCopy(BitConverter.GetBytes(shoobyBatA.attack), 0, monsterBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(shoobyBatA.defense), 0, monsterBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(shoobyBatA.regen), 0, monsterBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(shoobyBatA.health), 0, monsterBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(shoobyBatA.gold), 0, monsterBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(shoobyBatA.room_num), 0, monsterBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(shoobyBatA.dlen), 0, monsterBuffer, 46, 2);
                        client.Send(monsterBuffer);
                        client.Send(Encoding.UTF8.GetBytes(shoobyBatA.description));
 

                        //Player connections in room 7
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 7)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 7.");
                                }
                            }
                        }

                        //Room 7 connections
                        foreach (RoomConnection conn in room7Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }
                        lootCall = false;
                        fightActive = false; // Turn off fight if it was called

                    }
                    //************************************//
                    //*           Room 8                 *//
                    //************************************//
                    else if(8 == character.room_num) //If we are in room 8 Loft
                    {
                        Console.WriteLine("Activity in room Eight - Loft");
                        //Send room 8
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room8.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room8.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room8.name), 0, roomBuffer, 3, room8.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room8.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room8.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Player connections in room 8
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 8)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 8.");
                                }
                            }
                        }

                        //Room 8 connections
                        foreach (RoomConnection conn in room8Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }

                        lootCall = false;
                        fightActive = false; // Turn off fight if it was called

                    }
                    //************************************//
                    //*           Room 9                 *//
                    //************************************//
                    else if(9 == character.room_num) //If we are in room 9 Dual Arena
                    {
                        Console.WriteLine("Activity in room Nine - Dual Arena");
                        //Send room 9
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room9.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room9.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room9.name), 0, roomBuffer, 3, room9.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room9.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room9.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Player connections in room 9
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 9)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 9.");
                                }
                            }
                        }

                        //Room 9 connections
                        foreach (RoomConnection conn in room9Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }

                        lootCall = false;
                        fightActive = false; // Turn off fight if it was called

                    }
                    //************************************//
                    //*           Room 10                *//
                    //************************************//
                    else if(10 == character.room_num) //If we are in room 10 Holding Cell
                    {

                        if (fightActive == true)
                        {
                            int playerDamage = Math.Max(0, character.attack - froobyA.defense);
                            int monsterDamage = Math.Max(0, froobyA.attack - character.defence);

                            froobyA.health -= (short)playerDamage;
                            character.health -= (short)monsterDamage;

                            froobyA.health += (short)(froobyA.regen * 10 / 100);
                            character.health += (short)(character.regen + 10 / 100);

                            if(froobyA.health <= 0){
                                Console.WriteLine("Frooby monster has died. ROOM 4");
                                //Make sure to change flags to show dead monster
                                froobyA.flag = 0x78; //this should indicate the monster is dead.
                            }

                            if (character.health <= 0)
                            {
                                //This player should die
                                Console.WriteLine($"{character.name} was killed in room 7");
                                character.flags = 0x00;
                                playerDead = true;
                            }
                        }

                        if (lootCall == true && froobyA_Dead == true)
                        {
                            lootRoom10 = lootRoom10 + 1;

                            if (lootRoom10 ==  1)
                            {
                                ushort prevGold = character.gold;
                                ushort goldGained = 200;
                                ushort newGold = (ushort)(prevGold + goldGained);
                                character.gold = newGold;
                                Console.WriteLine($"200 gold was looted by {character.name} in room 10");
                            }
                            else
                            {
                                Console.WriteLine($"{character.name} attempted to loot twice in room 10");
                                rejected_packet = 11;
                            }
                        }

                        Console.WriteLine("Activity in room Ten - Holding Cell");
                        //Send room 10
                        byte[] roomBuffer = new byte[37];
                        roomBuffer[0] = room10.type;
                        Buffer.BlockCopy(BitConverter.GetBytes(room10.num), 0, roomBuffer, 1, 2);
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(room10.name), 0, roomBuffer, 3, room10.name.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(room10.desc_length), 0, roomBuffer, 35, 2);
                        client.Send(roomBuffer); //Send room Number: 1, Half Bathroom: 104
                        client.Send(Encoding.UTF8.GetBytes(room10.desc)); // Send Description

                        //Send Character
                        byte[] charBuffer = new byte[48];
                        charBuffer[0] = 10; //starts with 10 to read it in correctly.
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(character.name), 0, charBuffer, 1, character.name.Length);
                        charBuffer[33] = character.flags;
                        Buffer.BlockCopy(BitConverter.GetBytes(character.attack), 0, charBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.defence), 0, charBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.regen), 0, charBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.health), 0, charBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.gold), 0, charBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.room_num), 0, charBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(character.desc_length), 0, charBuffer, 46, 2);
                        client.Send(charBuffer);
                        client.Send(Encoding.UTF8.GetBytes(character.desc));

                        //Sending monsters in room 10
                        byte[] monsterBuffer = new byte[48];
                        monsterBuffer[0] = froobyA.type; // Use the actual type value of the monster packet
                        Buffer.BlockCopy(Encoding.UTF8.GetBytes(froobyA.name), 0, monsterBuffer, 1, froobyA.name.Length);
                        monsterBuffer[33] = froobyA.flag;
                        Buffer.BlockCopy(BitConverter.GetBytes(froobyA.attack), 0, monsterBuffer, 34, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(froobyA.defense), 0, monsterBuffer, 36, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(froobyA.regen), 0, monsterBuffer, 38, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(froobyA.health), 0, monsterBuffer, 40, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(froobyA.gold), 0, monsterBuffer, 42, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(froobyA.room_num), 0, monsterBuffer, 44, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(froobyA.dlen), 0, monsterBuffer, 46, 2);
                        client.Send(monsterBuffer);
                        client.Send(Encoding.UTF8.GetBytes(froobyA.description));


                        //Player connections in room 10
                        foreach(var player in PlayerStruct)
                        {
                            if (player.room_num == 10)
                            {
                                if (player.name != character.name) //Checks for host player
                                {
                                    byte[] playBuffer = new byte[48];
                                    playBuffer[0] = 10; //starts with 10 to read it in correctly.
                                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(player.name), 0, playBuffer, 1, player.name.Length);
                                    playBuffer[33] = player.flags;
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.attack), 0, playBuffer, 34, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.defence), 0, playBuffer, 36, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.regen), 0, playBuffer, 38, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.health), 0, playBuffer, 40, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.gold), 0, playBuffer, 42, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.room_num), 0, playBuffer, 44, 2);
                                    Buffer.BlockCopy(BitConverter.GetBytes(player.desc_length), 0, playBuffer, 46, 2);
                                    client.Send(playBuffer);
                                    client.Send(Encoding.UTF8.GetBytes(player.desc));

                                    Console.WriteLine($"Character {player.name} is in room 10.");
                                }
                            }
                        }

                        //Room 10 connections
                        foreach (RoomConnection conn in room10Pack.connections)
                        {
                            ConnectionPacket connection = new ConnectionPacket(conn.RoomNumber, conn.RoomName, conn.Description);
                            client.Send(connection.Serialize());
        
                        }

                        lootCall = false;
                        fightActive = false; // Turn off fight if it was called

                    }

                    lootCall = false;
                    room_activity = false; //End room activity
                }
                
                if (rejected_packet == 1) //This is for rejected Stats
                {
                    RejectPacket rejectPacket = new RejectPacket();
                    rejectPacket.error_code = 4;
                    rejectPacket.err_msg = "Character Stats are too high, Please try again!";
                    rejectPacket.err_msg_length = (ushort)rejectPacket.err_msg.Length;

                    byte[] rejectBuffer = new byte[4 + rejectPacket.err_msg_length];// changed to 4
                    rejectBuffer[0] = rejectPacket.type;
                    rejectBuffer[1] = rejectPacket.error_code;
                    Buffer.BlockCopy(BitConverter.GetBytes(rejectPacket.err_msg_length), 0, rejectBuffer, 2, 2);
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(rejectPacket.err_msg), 0, rejectBuffer, 4, rejectPacket.err_msg_length);
                    client.Send(rejectBuffer);
                    Console.WriteLine("RejectStats Buffer Reached");

                    rejected_packet = 0;
                }
                else if (rejected_packet ==  2) //This is for an unexpected packet
                {
                    Console.WriteLine($"Unexpected packet received. Type: {characterBeginning[0]}");

                    // Handle the unexpected packet type
                    // For simplicity, let's send a rejection packet
                    RejectPacket rejectPacket = new RejectPacket();
                    rejectPacket.error_code = 0; // Define appropriate error code for unexpected packet type
                    rejectPacket.err_msg = "\nUnexpected packet type.\n";
                    rejectPacket.err_msg_length = (ushort)rejectPacket.err_msg.Length;

                    byte[] rejectBuffer = new byte[4 + rejectPacket.err_msg_length]; //changed to 4
                    rejectBuffer[0] = rejectPacket.type;
                    rejectBuffer[1] = rejectPacket.error_code;
                    Buffer.BlockCopy(BitConverter.GetBytes(rejectPacket.err_msg_length), 0, rejectBuffer, 2, 2);
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(rejectPacket.err_msg), 0, rejectBuffer, 4, rejectPacket.err_msg_length);
                    client.Send(rejectBuffer);
                    
                    Console.WriteLine("Reject Statement: Unexpected packet");
                    // This just an extra if statement for error handeling

                    rejected_packet = 0;

                    continue;
                }
                else if (rejected_packet == 3) //This is for if game is not started
                {
                    RejectPacket rejectPacket = new RejectPacket();
                    rejectPacket.error_code = 5;
                    rejectPacket.err_msg = "Not ready, START is most likely not enabled.";
                    rejectPacket.err_msg_length = (ushort)rejectPacket.err_msg.Length;

                    byte[] rejectBuffer = new byte[4 + rejectPacket.err_msg_length];// changed to 4
                    rejectBuffer[0] = rejectPacket.type;
                    rejectBuffer[1] = rejectPacket.error_code;
                    Buffer.BlockCopy(BitConverter.GetBytes(rejectPacket.err_msg_length), 0, rejectBuffer, 2, 2);
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(rejectPacket.err_msg), 0, rejectBuffer, 4, rejectPacket.err_msg_length);
                    client.Send(rejectBuffer);
                    Console.WriteLine("RejectNotReady Buffer Reached");

                    rejected_packet = 0;
                }
                else if (rejected_packet == 4) //Bad Room error
                {
                    RejectPacket rejectPacket = new RejectPacket();
                    rejectPacket.error_code = 1;
                    rejectPacket.err_msg = "Bad Room, this room does not exist. or not connected";
                    rejectPacket.err_msg_length = (ushort)rejectPacket.err_msg.Length;

                    byte[] rejectBuffer = new byte[4 + rejectPacket.err_msg_length];// changed to 4
                    rejectBuffer[0] = rejectPacket.type;
                    rejectBuffer[1] = rejectPacket.error_code;
                    Buffer.BlockCopy(BitConverter.GetBytes(rejectPacket.err_msg_length), 0, rejectBuffer, 2, 2);
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(rejectPacket.err_msg), 0, rejectBuffer, 4, rejectPacket.err_msg_length);
                    client.Send(rejectBuffer);
                    Console.WriteLine("RejectBadRoom Buffer Reached");

                    rejected_packet = 0;
                }
                else if (rejected_packet == 5) //PVP Not implimented
                {
                    RejectPacket rejectPacket = new RejectPacket();
                    rejectPacket.error_code = 8;
                    rejectPacket.err_msg = "PVP is not implimented currently in this server.";
                    rejectPacket.err_msg_length = (ushort)rejectPacket.err_msg.Length;

                    byte[] rejectBuffer = new byte[4 + rejectPacket.err_msg_length];// changed to 4
                    rejectBuffer[0] = rejectPacket.type;
                    rejectBuffer[1] = rejectPacket.error_code;
                    Buffer.BlockCopy(BitConverter.GetBytes(rejectPacket.err_msg_length), 0, rejectBuffer, 2, 2);
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(rejectPacket.err_msg), 0, rejectBuffer, 4, rejectPacket.err_msg_length);
                    client.Send(rejectBuffer);
                    Console.WriteLine("Reject No PVP Buffer Reached");

                    rejected_packet = 0;
                }
                else if (rejected_packet ==  11) //This is for trying to loot twice
                {
                    Console.WriteLine($"Trying to loot twice. Type: {characterBeginning[0]}");

                    // For simplicity, let's send a rejection packet
                    RejectPacket rejectPacket = new RejectPacket();
                    rejectPacket.error_code = 0; // Define appropriate error code for unexpected packet type
                    rejectPacket.err_msg = "You cannot loot more than once after event!";
                    rejectPacket.err_msg_length = (ushort)rejectPacket.err_msg.Length;

                    byte[] rejectBuffer = new byte[4 + rejectPacket.err_msg_length]; //changed to 4
                    rejectBuffer[0] = rejectPacket.type;
                    rejectBuffer[1] = rejectPacket.error_code;
                    Buffer.BlockCopy(BitConverter.GetBytes(rejectPacket.err_msg_length), 0, rejectBuffer, 2, 2);
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(rejectPacket.err_msg), 0, rejectBuffer, 4, rejectPacket.err_msg_length);
                    client.Send(rejectBuffer);
                    
                    Console.WriteLine("Reject Statement: Double loot");
                    // This just an extra if statement for error handeling

                    rejected_packet = 0;

                    continue;
                }
                else if (rejected_packet ==  12) //This is when there is nothing to loot yet
                {
                    Console.WriteLine($"Nothing to loot yet. Type: {characterBeginning[0]}");

                    // For simplicity, let's send a rejection packet
                    RejectPacket rejectPacket = new RejectPacket();
                    rejectPacket.error_code = 0; // Define appropriate error code for unexpected packet type
                    rejectPacket.err_msg = "Nothing to loot yet. Defeat something and try again. or there is no events in this room.";
                    rejectPacket.err_msg_length = (ushort)rejectPacket.err_msg.Length;

                    byte[] rejectBuffer = new byte[4 + rejectPacket.err_msg_length]; //changed to 4
                    rejectBuffer[0] = rejectPacket.type;
                    rejectBuffer[1] = rejectPacket.error_code;
                    Buffer.BlockCopy(BitConverter.GetBytes(rejectPacket.err_msg_length), 0, rejectBuffer, 2, 2);
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(rejectPacket.err_msg), 0, rejectBuffer, 4, rejectPacket.err_msg_length);
                    client.Send(rejectBuffer);
                    
                    Console.WriteLine("Reject Statement: Nothing to loot yet");
                    // This just an extra if statement for error handeling

                    rejected_packet = 0;

                    continue;
                }
            }
        
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while handling client: {ex.Message}");
            Console.WriteLine("Exception Bracket Reached");
        }
        Console.WriteLine("Ending thread for client");
        client.Close();
    }   
}
