using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LeeboxLib
{
    public static class LeeboxManager
    {
        public static string Address { get; private set; } = "https://localhost:7256/api";

        public static HttpClient client { get; private set; } = new HttpClient();

        /// <summary>
        /// Use this to setup where the Leebox API is located.
        /// </summary>
        /// <param name="address"></param>
        public static void Initialize(string address)
        {
            Address = address;
        }

        /// <summary>
        /// Use this to create a room with a specified maximum number of players.
        /// </summary>
        /// <param name="MaxPlayers"></param>
        /// <returns></returns>
        public static async Task<Room> CreateRoom(int MaxPlayers)
        {
            Room room = new Room(MaxPlayers);
            await room.Setup();
            return room;
        }
    }
}
