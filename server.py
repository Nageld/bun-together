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
        delete(userID,sender, outMessage, burrow)
    elif message['action'] == "move":
        move(userID,sender, outMessage, burrow)


def delete(userID,sender, outMessage, burrow):
    if userID in burrow2users[burrow]:
        for i in usersinrooms[burrow][:]:  # Use a copy of the list to avoid modifying it during iteration
            if i[0] != sender:
                UDPServerSocket.sendto(outMessage, i[0])
            else:
                usersinrooms[burrow].remove(i)
        burrow2users[burrow].remove(userID)

def move(userID,sender, outMessage, burrow):
    if userID not in burrow2users[burrow]:
        for i in usersinrooms[burrow][:]:
            UDPServerSocket.sendto(i[1], sender)
            UDPServerSocket.sendto(outMessage, i[0])
        burrow2users[burrow].append(userID)
        usersinrooms[burrow].append((sender, outMessage))
    else:
        for i in usersinrooms[burrow][:]:
            if i[0] == sender:
                usersinrooms[burrow].remove(i)
            else:
                UDPServerSocket.sendto(outMessage, i[0])
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
