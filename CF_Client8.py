import tkinter as tk
from tkinter import scrolledtext
import socket
import threading
import struct
import sys
import threading

# Declare client_socket as a global variable
client_socket = None

#a lot works, but I am not recieiving more than one character, and also still not recieiving any
#connection packets as well.


def connect_to_server(host, port):
    global client_socket, connected
    try:
        # Create a socket object
        client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        # Connect to the server
        client_socket.connect((host, port))
        print("Connected to server:", (host, port))
        connected = True

        # Start a thread to receive messages from the server
        threading.Thread(target=receive_messages).start()
    except Exception as e:
        print("Error:", e)

def parse_game_message(a, b, c, d):
    # Unpack the bytes according to the GAME message protocol
    initial_points = a
    stat_limit = b
    description_length = d
    description = d

    # Display game information in the main text box
    text_box.insert("end", f"Initial Points: {initial_points}\n")
    text_box.insert("end", f"Stat Limit: {stat_limit}\n")
    text_box.insert("end", f"Description: {description}\n")
    text_box.see("end")  # Scroll to the end of the text box

def send_message(event=None):
    global client_socket
    message = user_input.get("1.0", "end-1c")  # Get message from user input field
    user_input.delete("1.0", "end")
    try:
        if message.strip().lower() == "#help":
            display_help()
            print("Help being displayed")
        elif message.strip().lower() == "#char":
            enter_character_details()
            print("Client called to create character")
        elif message.strip().lower() == "#start":  # Check if user wants to start playing
            send_start()
            print("Client sending start to server")
        elif message.strip().lower() == "#err":  # Check if user wants to display error codes
            display_error_codes()
            print("Error menu being displayed")
            return
        elif message.strip().lower() == "#fight":  # Check if user wants to initiate a fight
            send_fight()  # Just send the FIGHT message to the server
            print("Client called for fight")
        elif message.strip().lower() == "#change":  # Check if user wants to change rooms
            enter_room_to_change()  # Prompt for room change
            print("Client called to change rooms")
        elif message.strip().lower() == "#loot":  # Check if user wants to loot
            loot_prompt()
            return
        elif message.strip().lower() == "#clear":  # Check if user wants to loot
            clear_main_text_box()
            return
        elif message.strip().lower() == "#clearconn":  # Check if user wants to loot
            clear_text_box1
            return
        elif message.strip().lower() == "#clearchar":  # Check if user wants to loot
            clear_text_box2
            return
        elif message.lower().startswith("#mesg"):
            # Extract the recipient's name from the message
            parts = message.split(" ")
            if len(parts) >= 2:
                recipient_name = parts[1]
                # Prompt the user to enter the message content
                user_input.config(state=tk.NORMAL)
                user_input.delete("1.0", "end")
                user_input.insert("end", "Enter message: ")
                
                # Define a function to send the message to the recipient
                def send_message_to_recipient_with_content(event):
                    content = user_input.get("1.0", "end-1c").strip()
                    print(f"Message Content: {content}")
                    send_message_to_recipient(recipient_name, content)
                
                # Bind the function to the Enter key
                user_input.bind("<Return>", send_message_to_recipient_with_content)
            else:
                print("Recipient's name not provided.")

        # Send message to the server
        #client_socket.send(message.encode("utf-8")) #I dont need to send this data to server
        # Optionally, display the sent message in the main text box
        text_box.insert("end", "User Input: " + message + "\n")
        text_box.see("end")  # Scroll to the end of the text box
    except Exception as e:
        print("Error sending message:", e)

def receive_messages():
    global client_socket, connected
    while connected:
        try:
            # Receive data from the server
            message_type = client_socket.recv(1)
            if not message_type:
                print("if not data reached")
                break
            # Process the received data based on its type
            #message_type = data[0]
            message_type = ord(message_type)
            if message_type == 10:
                data = client_socket.recv(47)

                player_name = data[0:32].decode("utf-8").strip('\0')
                player_flags = data[32]
                player_attack = struct.unpack("<H", data[33:35])[0]
                player_defense = struct.unpack("<H", data[35:37])[0]
                player_regen = struct.unpack("<H", data[37:39])[0]
                player_health = struct.unpack("<h", data[39:41])[0]
                player_gold = struct.unpack("<H", data[41:43])[0]
                player_room_number = struct.unpack("<H", data[43:45])[0]
                description_length = struct.unpack("<H", data[45:47])[0]

                desc_data = client_socket.recv(description_length)

                player_description = desc_data.decode("utf-8")

                process_character_packet(player_name, player_flags, player_attack, player_defense, player_regen, player_health, player_gold, player_room_number, description_length, player_description)
                print("Type character recieved")
            elif message_type == 1:  # MESSAGE type
                # Receive the entire message packet
                #this works with messages, but didn't seem to be working on seths server.

                data = client_socket.recv(66)
                if not data:
                    print("No message packet data received")
                    break


                # Unpack the received data
                message_length = struct.unpack("<H", data[0:2])[0]
                #recipient_name_bytes = data[2:34]
                recipient_name = data[2:34].decode("utf-8").strip('\0')
                #sender_name_bytes = data[34:64]
                sender_name = data[34:64].decode("utf-8").strip('\0')
                is_narration = struct.unpack("<BB", data[64:66])

                desc_data = client_socket.recv(message_length)

                message = desc_data.decode("utf-8")

                # Process the parsed message data
                process_message(recipient_name, sender_name, is_narration, message)
                print("Message received")
            elif message_type == 8:
                # ACCEPT packet received
                process_accept_packet()
                print("Accept packet recieved")
            elif message_type == 7:
                # REJECT packet received
                process_reject_packet()
                print("Reject/Error packet recieved")
            elif message_type == 11: #Game packet
                data = client_socket.recv(6)
                initial_points = struct.unpack("<H", data[0:2])[0]
                stat_limit = struct.unpack("<H", data[2:4])[0]
                description_length = struct.unpack("<H", data[4:6])[0]
                
                desc_data = client_socket.recv(description_length)

                description = desc_data.decode("utf-8")

                parse_game_message(initial_points, stat_limit, description_length, description)

                print("Game message recieved")
            elif message_type == 14: # VERSION packet
                try:
                    # Receive the version data from the server
                    version_data = client_socket.recv(5)
                    
                    # Unpack the version data
                    major_revision = struct.unpack("<B", version_data[0:1])[0]
                    minor_revision = struct.unpack("<B", version_data[1:2])[0]
                    extension_list_size = struct.unpack("<H", version_data[2:4])[0]

                    # Receive the extension list if size is not zero
                    extensions = []
                    if extension_list_size > 0:
                        extensions_data = client_socket.recv(extension_list_size)
                        while extensions_data:
                            extension_length = struct.unpack("<H", extensions_data[0:2])[0]
                            extension = extensions_data[2:2+extension_length].decode("utf-8")
                            extensions.append(extension)
                            extensions_data = extensions_data[2+extension_length:]

                    # Print or process the received version information
                    print("Received version information:")
                    print("LURK Major Revision:", major_revision)
                    print("LURK Minor Revision:", minor_revision)
                    print("Extensions:", extensions)

                    text_box.insert("end", "Version Inforamtion\n")
                    text_box.insert("end", f"LURK Major Revision: {major_revision}\n")
                    text_box.insert("end", f"LURK Minor Revision: {minor_revision}\n")
                    text_box.insert("end", f"Extensions: {extensions}\n")
                    text_box.tag_config("green", foreground="green")
                    text_box.insert("end", f"VERSION: {major_revision}.{minor_revision}\n", "green")
                    text_box.see("end")  # Scroll to the end of the text box
                    
                    # Optionally, you can further process or use the version information here

                except Exception as e:
                    print("Error receiving version message:", e)

            elif message_type == 13:
                #for populating text box 3
                populate_connection_text_boxes()
                print("Type connection recieved")
            elif message_type == 9:
                #Processing a room packet
                parse_room_message()
                print("Type room recieved")
            else:
                print("else bracket reached")
                # Handle other message types here if needed
                pass
        except Exception as e:
            print("Error receiving message:", e)
            connected = False
            break


def on_connect_click():
    host = host_entry.get()
    port = int(port_entry.get())
    connect_to_server(host, port)

def on_closing_window():
    global client_socket
    if client_socket:
        client_socket.close()  # Close the socket connection
    root.destroy()
    sys.exit()  # Terminate the program

def enter_character_details():
    global character_attributes, current_attribute_index
    # Initialize the list of character attributes and the current attribute index
    character_attributes = {"Name": "", "Flags": "", "Attack": "", "Defense": "", "Regen": "", "Health": "", "Gold": "", "Room number": "", "Description": ""}
    current_attribute_index = 0
    
    # Change the behavior of the user input box to accept single line input
    user_input.config(state=tk.NORMAL)
    user_input.delete("1.0", "end")
    user_input.bind("<Return>", next_attribute)
    user_input.insert("end", f"Enter {list(character_attributes.keys())[current_attribute_index]}:")
    user_input.focus()

def next_attribute(event):
    global character_attributes, current_attribute_index
    # Get the attribute entered by the user
    attribute = user_input.get("1.0", "end-1c").strip()
    # Remove the newline character from the entered attribute
    attribute = attribute.replace("\n", "")
    # Store the entered attribute in the list
    attribute_name = list(character_attributes.keys())[current_attribute_index]
    # Extract the entered value after the colon (if present)
    if ":" in attribute:
        attribute = attribute.split(":")[-1].strip()
    character_attributes[attribute_name] = attribute
    
    # Prompt for the next attribute or send the character if all attributes have been entered
    if current_attribute_index < len(character_attributes) - 1:
        current_attribute_index += 1
        if list(character_attributes.keys())[current_attribute_index] == "Description":
            user_input.delete("1.0", "end")
            user_input.insert("end", f"Enter {list(character_attributes.keys())[current_attribute_index]}:")
        else:
            user_input.delete("1.0", "end")
            user_input.insert("end", f"Enter {list(character_attributes.keys())[current_attribute_index]}:")
    else:
        # Clear the user input text box
        user_input.delete("1.0", "end")
        send_character()

def process_message(recipient_name, sender_name, is_narration, message):
    # Display the message in the console
    print(f"Recipient: {recipient_name}")
    print(f"Sender: {sender_name}")
    print(f"Narration: {'Yes' if is_narration else 'No'}")
    print(f"Message: {message}")

    # Example: Display the message in a GUI text box
    # Assuming you have a text box named 'message_text_box' in your GUI
    text_box.config(state=tk.NORMAL)  # Set state to NORMAL to allow editing
    text_box.tag_config("blue", foreground="blue")
    text_box.insert(tk.END, f"From: {sender_name}\n", "blue")
    text_box.tag_config("orange", foreground="orange")
    text_box.insert(tk.END, f"To: {recipient_name}\n", "orange")
    text_box.insert(tk.END, f"Narration: {'Yes' if is_narration else 'No'}\n")
    text_box.insert(tk.END, f"Message: {message}\n\n")
    text_box.see("end")  # Scroll to the end of the text box
    text_box.config(state=tk.DISABLED)  # Set state back to DISABLED


def process_accept_packet():
    # Parse the type of action accepted
    action_accepted = client_socket.recv(1)
    action_accepted = ord(action_accepted)
    # You can handle different types of actions here if needed
    print("Server accepted action:", action_accepted)
    text_box.tag_config("green", foreground="green")
    text_box.insert("end", f"Accept Packet: {action_accepted}\n", "green")
    text_box.see("end")  # Scroll to the end of the text box

def process_reject_packet():
    data = client_socket.recv(3)
    # Parse the error code and message length
    error_code = data[0]
    message_length = struct.unpack("<H", data[1:3])[0]
    # Parse the error message
    desc_data = client_socket.recv(message_length)
    error_message = desc_data.decode("utf-8")
    # Print or process the received reject packet
    print("Received reject packet:")
    print("Error Code:", error_code)
    print("Error Message:", error_message)
    # Update the main text box to display the reject packet information
    text_box.tag_config("red", foreground="red")
    text_box.insert("end", "Received reject packet:\n", "red")
    text_box.insert("end", f"Error Code: {error_code}\n")
    text_box.insert("end", f"Error Message: {error_message}\n")
    text_box.see("end")  # Scroll to the end of the text box


#store the name of the host player
host_name = ""
host_room = ""

def send_character():
    global character_attributes, host_name
    # Revert the behavior of the user input box to its normal state
    user_input.config(state=tk.DISABLED)
    user_input.unbind("<Return>")
    
    try:
        # Check if all required attributes have been entered
        required_attributes = ["Name", "Flags", "Attack", "Defense", "Regen", "Health", "Gold", "Room number", "Description"]
       
        
        # Get the description and its length
        description = character_attributes["Description"]
        description_length = len(description)
        
        # Get the character attributes
        name = character_attributes["Name"]
        flags = int(character_attributes["Flags"], 16)  # Convert flags from hex string to integer
        attack = int(character_attributes["Attack"])
        defense = int(character_attributes["Defense"])
        regen = int(character_attributes["Regen"])
        health = int(character_attributes["Health"])
        gold = int(character_attributes["Gold"])
        room_number = int(character_attributes["Room number"])

        host_name = name
        
        # Encode character attributes based on the protocol
        name_encoded = name.ljust(32, '\0').encode("utf-8")
        flags_encoded = flags.to_bytes(1, byteorder='big')
        attack_encoded = attack.to_bytes(2, byteorder='little')
        defense_encoded = defense.to_bytes(2, byteorder='little')
        regen_encoded = regen.to_bytes(2, byteorder='little')
        health_encoded = health.to_bytes(2, byteorder='little', signed=True)
        gold_encoded = gold.to_bytes(2, byteorder='little')
        room_number_encoded = room_number.to_bytes(2, byteorder='little')
        description_length_encoded = description_length.to_bytes(2, byteorder='little')
        description_encoded = description.encode("utf-8")
        
        # Pack the character data including description length and description
        character_data = (
            name_encoded +
            flags_encoded +
            attack_encoded +
            defense_encoded +
            regen_encoded +
            health_encoded +
            gold_encoded +
            room_number_encoded +
            description_length_encoded +
            description_encoded
        )

        # Add the message type (CHARACTER: 10) to the beginning of the packed data
        message_data = bytes([10]) + character_data

        # Send the packed data to the server
        client_socket.send(message_data)

        # Re-enable user input and bind the return key to send_message function
        user_input.config(state=tk.NORMAL)
        user_input.bind("<Return>", send_message)

        #Main text box shows that character creation has ended
        text_box.insert("end", "Character created, (If box doesn't populate enter #char again).\n")
        text_box.see("end")

        text_box.tag_config("green", foreground="green")
        text_box.insert("end", f"{host_name}, was created\n", "green")

    except Exception as e:
        print("Error sending character attributes:", e)

# These store host player attributes
'''
player_name = ""
player_flags = ""
player_attack = ""
player_defense = ""
player_regen = ""
player_health = ""
player_gold = ""
player_room_number = ""
player_description = ""
'''
#commented out

def process_character_packet(a,b,c,d,e,f,g,h,i,j):
    
    global host_name

    player_name = a
    player_flags = b
    player_attack = c
    player_defense = d
    player_regen = e
    player_health = f
    player_gold = g
    player_room_number = h
    description_length = i
    player_description = j

  # Parse the character data according to the protocol
    
    text_box.tag_config("purple", foreground="purple")
    text_box.insert("end", f"{player_name}, Is connected\n", "purple")

    #this line checks for the host player, and if it is the host player the text will be green

    # Populate text_box1 with character attributes
    text_box1.config(state=tk.NORMAL)
    #text_box1.delete("1.0", tk.END) #deletes text in box
    if player_name == host_label:
        text_box1.config(state=tk.NORMAL)
        text_box.tag_config("green", foreground="green")
        text_box1.insert("end", f"Name: {player_name}\n", "green")
        text_box1.insert("end", f"Health: {player_health}\n", "green")
        text_box1.insert("end", f"Attack: {player_attack}\n", "green")
        text_box1.insert("end", f"Regen: {player_regen}\n", "green")
        text_box1.insert("end", f"Defense: {player_defense}\n", "green")
        text_box1.insert("end", f"Flags: {player_flags}\n", "green")
        text_box1.insert("end", f"Gold: {player_gold}\n", "green")
        text_box1.insert("end", "\n")
        text_box1.config(state=tk.DISABLED)

    else:
        text_box1.config(state=tk.NORMAL)
        text_box1.insert("end", f"Name: {player_name}\n")
        text_box1.insert("end", f"Health: {player_health}\n")
        text_box1.insert("end", f"Attack: {player_attack}\n")
        text_box1.insert("end", f"Regen: {player_regen}\n")
        text_box1.insert("end", f"Defense: {player_defense}\n")
        text_box1.insert("end", f"Flags: {player_flags}\n")
        text_box1.insert("end", f"Gold: {player_gold}\n")
        text_box1.insert("end", "\n")
        text_box1.config(state=tk.DISABLED)

    if player_health <= 0:
        text_box.tag_config("red", foreground="red")
        text_box.insert("end", f"{player_name}, HAS DIED\n", "red")
        print("A player has reached death")

    text_box.tag_config("green", foreground="green")
    text_box.insert("end", f"{host_name}, Is the host player\n", "green")

    # Recieved character information
    print("Received character:")
    print("Name:", player_name)
    print("Flags:", player_flags)
    print("Attack:", player_attack)
    print("Defense:", player_defense)
    print("Regen:", player_regen)
    print("Health:", player_health)
    print("Gold:", player_gold)
    print("Room number:", player_room_number)
    print("Description:", player_description)

def enter_room_to_change():
    global user_input
    # Change the behavior of the user input box to accept single line input
    user_input.config(state=tk.NORMAL)
    user_input.bind("<Return>", send_message)
    user_input.delete("1.0", "end")
    user_input.bind("<Return>", send_room_change_request)
    user_input.insert("end", "Enter a room to change to:")
    user_input.focus()

def send_room_change_request(event=None):
    global user_input
    try:
        # Get the room number entered by the user and remove the prompt text
        room_input = user_input.get("1.0", "end-1c")
        room_number = room_input.split(":")[-1].strip()  # Extract the room number
        # Send the CHANGEROOM message to the server
        print(f"room number: {room_number}")
        text_box.insert("end", f"{room_number}, was changed to\n")
        send_change_room(int(room_number))
    except Exception as e:
        print("Error sending room change request:", e)
    finally:
        # Revert the behavior of the user input box to its normal state
        user_input.config(state=tk.NORMAL)
        user_input.unbind("<Return>")
        user_input.delete("1.0", "end")
        user_input.bind("<Return>", send_message)

def send_message_to_recipient(recipient_name, message_content):
    global client_socket

    try:
        # Construct the message packet
        message_type = bytes([1])  # Message type
        message_length = len(message_content).to_bytes(2, byteorder="little")  # Message length
        recipient_name_bytes = recipient_name.ljust(32, '\0').encode("utf-8")  # Recipient name
        sender_name_bytes = host_name.ljust(30, '\0').encode("utf-8")  # Sender name
        end_of_sender_name = bytes([0])  # End of sender name marker
        message_content_bytes = message_content.encode("utf-8")  # Message content

        # Concatenate the message packet parts
        message_packet = (
            message_type +
            message_length +
            recipient_name_bytes +
            sender_name_bytes +
            end_of_sender_name +
            message_content_bytes
        )

        # Send the message packet to the server
        client_socket.sendall(message_packet)

        # Revert the user input box back to send_message to accept other commands
        user_input.config(state=tk.NORMAL)
        user_input.bind("<Return>", send_message)
        user_input.delete("1.0", "end")
    except Exception as e:
        print("Error sending message to recipient:", e)


def send_change_room(room_number):
    try:
        # Pack the room number into bytes
        room_number_bytes = room_number.to_bytes(2, byteorder='little')
        # Send the CHANGEROOM message to the server
        client_socket.send(bytes([2]) + room_number_bytes)
        print("Attempt to change to room", room_number)
        # Update the main text box to indicate that the change room message is sent
        text_box.insert("end", f"CHANGEROOM message sent to room {room_number}\n")
        text_box.see("end")  # Scroll to the end of the text box

        clear_text_box1() #clears player connection box

        clear_text_box2() #clears Room connection box

    except Exception as e:
        print("Error sending CHANGEROOM message:", e)

def loot_prompt():
    # Change the behavior of the user input box to accept single line input
    user_input.config(state=tk.NORMAL)
    user_input.delete("1.0", "end")
    user_input.bind("<Return>", send_loot_request)
    user_input.insert("end", "Enter the name of the target player to loot:")
    user_input.focus()


def send_loot_request(event=None):
    global user_input
    try:
        # Get the target player name entered by the user and remove the prompt text
        target_player = user_input.get("1.0", "end-1c").split(":")[-1].strip()
        # Send the LOOT message to the server
        print(f"Loot called attempting to loot {target_player}")
        text_box.insert("end", f"{target_player}, was called to be looted\n")
        send_loot(target_player)
    except Exception as e:
        print("Error sending loot request:", e)
    finally:
        # Revert the behavior of the user input box to its normal state
        user_input.config(state=tk.NORMAL)
        user_input.unbind("<Return>")
        user_input.delete("1.0", "end")
        user_input.bind("<Return>", send_message)

def send_loot(target_player):
    try:
        # Prepare the LOOT message data
        loot_message = bytes([5]) + target_player.ljust(32, '\0').encode("utf-8")
        # Send the LOOT message to the server
        client_socket.send(loot_message)
    except Exception as e:
        print("Error sending LOOT message:", e)


#Displays help
def display_help():
    # Display list of commands in the main text box
    text_box.tag_config("green", foreground="green")
    text_box.insert("end", "List of commands:\n", "green")
    text_box.insert("end", "press connect <host> <port>: Connect to a server\n")
    text_box.insert("end", "#help: Display list of commands\n")
    text_box.insert("end", "#char: Create a character\n")
    text_box.insert("end", "#mesg (recipient name): Send a message to someone. no'()'\n")
    text_box.insert("end", "#start: Tell the server to start the game\n")
    text_box.insert("end", "#err: Displays list of errors and what they mean\n")
    text_box.insert("end", "#fight: Calls player to 'fight' in the room\n")
    text_box.insert("end", "#change: Change the room player is in\n")
    text_box.insert("end", "#loot: Loot monsters, players, or items in a room\n")
    text_box.insert("end", "#clear: clears the main text box\n")
    text_box.insert("end", "#clearconn: clears the room connection text box\n")
    text_box.insert("end", "#clearchar: clears the character connection text box\n")
    
    # Add more commands as needed
    text_box.see("end")  # Scroll to the end of the text box

def clear_main_text_box():
    # Clear the main text box
    text_box.delete('1.0', 'end')

def display_error_codes():
    # Define error codes and their meanings
    error_codes = {
        0: "Other: Unknown error depending on server",
        1: "Bad Room: Room doesn't exist or not connected",
        2: "Player Exist: Tried to create duplicate player",
        3: "Bad Monster: Trying to loot non-existant monster",
        4: "Stat Error: Stats are not correct for server",
        # Add more error codes and their meanings as needed
    }
    
    # Display error codes and their meanings in the text box
    text_box.config(state=tk.NORMAL)
    text_box.tag_config("red", foreground="red")
    text_box.insert(tk.END, "Error Codes:\n", "red")
    for code, meaning in error_codes.items():
        text_box.insert(tk.END, f"Error {code}: {meaning}\n")
    text_box.see("end")
    
def send_fight():
    global host_name
    try:
        # Send the FIGHT message to the server
        client_socket.send(bytes([3]))
        # Update the main text box to indicate that the fight message is sent
        text_box.insert("end", f"{host_name}, Initiated fight\n")
        text_box.see("end")  # Scroll to the end of the text box

        # Re-enable user input and bind the return key to send_message function
        user_input.config(state=tk.NORMAL)
        user_input.bind("<Return>", send_message)

        #clear_text_box1() #clears the player connection box
        #clear_text_box2() #clears room connection box
    except Exception as e:
        print("Error sending FIGHT message:", e)

def send_start():
    global text_box1
    try:
        # Send the START message to the server
        client_socket.send(bytes([6]))
        # Update the main text box to indicate that START message is sent
        text_box.insert("end", "START message sent\n")
        text_box.see("end")  # Scroll to the end of the text box

        # Re-enable user input and bind the return key to send_message function
        user_input.config(state=tk.NORMAL)
        user_input.bind("<Return>", send_message)

        clear_text_box1()
        clear_text_box2()
    except Exception as e:
        print("Error sending START message:", e)


def parse_room_message():
    # global text_box
    print("Got inside room message")
    data = client_socket.recv(36)
    # Extract room information from the data
    room_num = int.from_bytes(data[0:2], byteorder='little')
    room_name = data[2:34].decode('utf-8').strip('\0')
    room_desc_length = int.from_bytes(data[34:36], byteorder='little')

    desc_data = client_socket.recv(room_desc_length)

    room_desc = desc_data.decode("utf-8")

    # Display room information in the main text box
    text_box.config(state=tk.NORMAL)
    text_box.tag_config("blue", foreground="blue")
    text_box.insert("end", f"Current Room: {room_name}\n", "blue")
    text_box.tag_config("orange", foreground="orange")
    text_box.insert("end", f"Room Number: {room_num}\n", "orange")
    text_box.insert("end", f"Room Description: {room_desc}\n")
    text_box.see("end")
    text_box.config(state=tk.DISABLED)
    


def populate_connection_text_boxes():
    
    data = client_socket.recv(36)
    # Extract room number, name, and description
    room_number = int.from_bytes(data[0:2], byteorder='little')
    room_name = data[2:34].decode('utf-8').strip('\0')
    room_desc_length = int.from_bytes(data[34:36], byteorder='little')

    desc_data = client_socket.recv(room_desc_length)

    room_description = desc_data.decode("utf-8")
    
    # Print variables to the console
    print("Room Number:", room_number)
    print("Room Name:", room_name)
    print("Room Description:", room_description)
    
    # Prepare text for text box three
    text_box3_data = f"R#: {room_number} | {room_name} |\n"
    
    # Prepare text for main text box
    main_text_box_data = f"{room_name}: {room_description}\n"
    
    # Update text box three
    text_box2.config(state=tk.NORMAL)
    text_box2.insert(tk.END, text_box3_data)
    text_box2.config(state=tk.DISABLED)
    #text_box2.delete("1.0", tk.END) #clears Room connection box

    # Update main text box
    text_box.config(state=tk.NORMAL)
    text_box.insert(tk.END, main_text_box_data)
    text_box.config(state=tk.DISABLED)

def clear_text_box1():
    text_box1.config(state=tk.NORMAL)  # Set state to NORMAL to allow editing
    text_box1.delete("1.0", tk.END)    # Delete all text content
    text_box1.config(state=tk.DISABLED)  # Set state back to DISABLED

def clear_text_box2():
    text_box2.config(state=tk.NORMAL)  # Set state to NORMAL to allow editing
    text_box2.delete("1.0", tk.END)    # Delete all text content
    text_box2.config(state=tk.DISABLED)  # Set state back to DISABLED



#STYLING OF TKINTER GUI
root = tk.Tk()
root.geometry("800x600")
root.title("CF LURK GUI")

# Host Entry
host_label = tk.Label(root, text="Host:")
host_label.grid(row=0, column=0, sticky="w", padx=5, pady=5)
host_entry = tk.Entry(root)
host_entry.grid(row=0, column=1, sticky="w", padx=5, pady=5)

# Port Entry
port_label = tk.Label(root, text="Port:")
port_label.grid(row=1, column=0, sticky="w", padx=5, pady=5)
port_entry = tk.Entry(root)
port_entry.grid(row=1, column=1, sticky="w", padx=5, pady=5)

# Connect Button
connect_button = tk.Button(root, text="Connect", command=on_connect_click)
connect_button.grid(row=2, column=0, columnspan=2, sticky="w", padx=5, pady=5)

# Main Text Box with Scrollbar
frame = tk.Frame(root)
frame.grid(row=3, column=0, padx=10, pady=10, sticky="nsew")

text_box = scrolledtext.ScrolledText(frame, width=60, height=20)
text_box.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
text_box.tag_config("blue", foreground="blue")
text_box.insert("end", "This is the main text box.\nIt displays data. type '#help' and press <Enter> for commands\n", "blue")

scrollbar = tk.Scrollbar(frame, orient=tk.VERTICAL, command=text_box.yview)
scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
text_box.config(yscrollcommand=scrollbar.set)

# User Input Field
user_input = tk.Text(root, width=60, height=3)
user_input.grid(row=4, column=0, padx=10, pady=5, sticky="w")
user_input.bind("<Return>", send_message) #Now I can use enter and be fast

# Send Button
send_button = tk.Button(root, text="Send Info Here")
send_button.grid(row=4, column=0, padx=10, pady=5, sticky="e")

# Additional Text Boxes (Display Only)
text_box1 = tk.Text(root, width=20, height=10, state="disabled")
text_box1.grid(row=3, column=1, padx=10, pady=10, sticky="nsew")
text_box1.insert("end", "This is text box 1.\nIt displays data.")

text_box2 = tk.Text(root, width=20, height=4, state="disabled")
text_box2.grid(row=4, column=1, padx=10, pady=10, sticky="nsew")
text_box2.insert("end", "This is text box 2.\nIt displays data.")

root.protocol("WM_DELETE_WINDOW", on_closing_window)  # Handle window close event

root.mainloop()
