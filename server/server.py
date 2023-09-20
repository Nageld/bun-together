import json
import socket

burrow2users = {}
usersinrooms = {}

localPort = 25565
bufferSize = 1024
# Create a datagram socket
UDPServerSocket = socket.socket(family=socket.AF_INET, type=socket.SOCK_DGRAM)
# Bind to address and ip
UDPServerSocket.bind(('', localPort))
print("UDP server up and listening")


def handleMessage(message, sender, outMessage, burrow):
    userID = message['userID']
    if message['action'] == "delete":
        delete(userID, sender, outMessage, burrow)
    elif message['action'] == "move":
        move(userID, sender, outMessage, burrow)
    elif message['action'] == "users":
        users(userID, sender, burrow, message)
        
def users(userID, sender, burrow, message):
    filtered_rooms = [
    f"@ {key}" 
    for key, users in burrow2users.items()
    if not (key == burrow and len(users) == 1 and userID in users)
    ]
    roomString = "Rooms with players: " + " ".join(filtered_rooms)
    message['extra'] = roomString
    UDPServerSocket.sendto(str(message).encode("utf-8"), sender)

def delete(userID, sender, outMessage, burrow):
    if userID in burrow2users[burrow]:
        # Use a copy of the list to avoid modifying it during iteration
        for i in usersinrooms[burrow][:]:
            if i[0] != sender:
                UDPServerSocket.sendto(outMessage.encode("utf-8"), i[0])
            else:
                usersinrooms[burrow].remove(i)
        burrow2users[burrow].remove(userID)
        if len(burrow2users[burrow]) == 0:
            burrow2users.pop(burrow)


def move(userID, sender, outMessage, burrow):
    if userID not in burrow2users[burrow]:
        for i in usersinrooms[burrow][:]:
            UDPServerSocket.sendto(i[1].encode("utf-8"), sender)
            UDPServerSocket.sendto(outMessage.encode("utf-8"), i[0])
        burrow2users[burrow].append(userID)
        usersinrooms[burrow].append((sender, outMessage))
    else:
        for i in usersinrooms[burrow][:]:
            if i[0] == sender:
                usersinrooms[burrow].remove(i)
            else:
                UDPServerSocket.sendto(outMessage.encode("utf-8"), i[0])
        usersinrooms[burrow].append((sender, outMessage))
        
while True:
    bytesAddressPair = UDPServerSocket.recvfrom(bufferSize)
    message = bytesAddressPair[0]
    address = bytesAddressPair[1]

    location = message.decode("utf-8")
    ip, port = address
    sender = ip + ":" + str(port)

    ParsedMessage = json.loads(message)
    burrow = ParsedMessage['burrow']

    if burrow not in burrow2users:
        burrow2users[burrow] = []
        usersinrooms[burrow] = []

    handleMessage(ParsedMessage, address, location, burrow)
