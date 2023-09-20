using System;
using Misc;
using UnityEngine;

namespace MultiplayerDropIn;

public class Message
{
    private string position;
    private Guid userID;
    private string burrow;
    private Direction facing;
    private string action;
    private string extra;

    public String Position
    {
        get => position;
        set => position = value;
    }

    public Guid UserID
    {
        get => userID;
        set => userID = value;
    }

    public string Burrow
    {
        get => burrow;
        set => burrow = value;
    }

    public Direction Facing
    {
        get => facing;
        set => facing = value;
    }

    public string Action
    {
        get => action;
        set => action = value;
    }

    public string Extra
    {
        get => extra;
        set => extra = value;
    }


    public Message(string burrow, Guid userID, string position)
    {
        this.burrow = burrow;
        this.userID = userID;
        this.position = position;
    }


    public override string ToString()
    {
        return $@"{{ ""burrow"": ""{burrow}"",""userID"": ""{userID}"",""position"": ""{position}"",""facing"": ""{facing}"",""action"": ""{action}"",""extra"": ""{extra}"" }}";
    }

    public bool Equals(Message message)
    {
        return message.ToString() == this.ToString();
    }

    public Vector3 PositionVec()
    {
        var s = position.Substring(1, position.Length - 2);
        string[] parts = s.Split(new string[] { "," }, StringSplitOptions.None);
        return new Vector3(
            float.Parse(parts[0]),
            float.Parse(parts[1]),
            float.Parse(parts[2]));
    }
}