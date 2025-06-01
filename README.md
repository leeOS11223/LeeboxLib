# LeeboxLib

This library is for building games for [Leebox](https://leebox.hereticalstudios.co.uk/)!

![NuGet Version](https://img.shields.io/nuget/v/LeeBoxLib.svg)


Here is an example game:
```cs
using LeeboxLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLeeBoxGame
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            LeeboxManager.Initialize("https://leebox.hereticalstudios.co.uk/api");
            Room room = await LeeboxManager.CreateRoom(10); //create room with 10 player slots


            Console.WriteLine($"Room created with ID: {room.ID}. press enter to start game");
            Console.ReadLine();

            await room.SyncRoomData(); //run this when ever you want to pull the latest room data

            await room.SetLocked(true); // dont let anyone else join
            await room.SetImage("https://hereticalstudios.co.uk/images/me.jpg");

            Random random = new Random();
            foreach (KeyValuePair<Player, string> v in await room.AskAll("Say something you think is true to Lee."))
            {
                Console.WriteLine($"Player: {v.Key.playerName}, Response: {v.Value}");

                //coin flip
                if (random.Next(2) == 0)
                    await v.Key.Say("Lee probably agrees.");
                else
                    await v.Key.Say("Lee probably disagrees.");
            }
            Console.ReadLine();
        }
    }
}
```
