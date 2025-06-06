using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeeboxLib
{
    public class Room : IEnumerable<Player>
    {
        public int MaxPlayers { get; private set; }
        public string ID { get; private set; }
        public string SecretKey { get; private set; }
        public bool Locked { get; private set; }
        public int PlayerCount { get; private set; } = 0;
        public List<Player> Players { get; private set; } = new List<Player>();
        public Stack<Player> ReconnectedPlayers { get; private set; } = new Stack<Player>();
        public HttpClient client => LeeboxManager.client;

        [Obsolete("Use LeeboxManager.CreateRoom(int MaxPlayers) instead.")]
        public Room(int MaxPlayers = 10)
        {
            this.MaxPlayers = MaxPlayers;
        }

        public class SetupResponse
        {
            public string ID { get; set; }
            public string SecretKey { get; set; }

            public SetupResponse(string ID, string SecretKey)
            {
                this.ID = ID;
                this.SecretKey = SecretKey;
            }
        }

        internal async Task Setup()
        {
            HttpResponseMessage response = await client.GetAsync(LeeboxManager.Address + "/newroom");
            response.EnsureSuccessStatusCode();

            SetupResponse room = await response.Content.ReadFromJsonAsync<SetupResponse>();
            if (room != null)
            {
                ID = room.ID;
                SecretKey = room.SecretKey;
                client.DefaultRequestHeaders.Add("X-Api-Key", SecretKey);
            }
            else
            {
                throw new Exception("Failed to create room.");
            }
        }
        public class RoomResponse
        {
            public string id { get; set; }
            public int playerCount { get; set; }
            public bool Locked { get; set; }
            public List<Player> players { get; set; }
            public List<Player> reconnectedPlayers { get; set; }

            public RoomResponse(string ID, int playerCount, bool Locked, List<Player> players, List<Player> reconnectedPlayers)
            {
                this.id = ID;
                this.Locked = Locked;
                this.players = players;
                this.reconnectedPlayers = reconnectedPlayers;
                this.playerCount = playerCount;
            }
        }

        /// <summary>
        /// Used to synchronize the room data with the server.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task SyncRoomData()
        {
            if (string.IsNullOrEmpty(SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            HttpResponseMessage response = await client.GetAsync(LeeboxManager.Address + "/" + ID);
            response.EnsureSuccessStatusCode();

            RoomResponse room = await response.Content.ReadFromJsonAsync<RoomResponse>();
            if (room != null && room.id == ID)
            {
                PlayerCount = room.playerCount;
                Players = room.players.ToList();
                Locked = room.Locked;
                foreach (var player in Players)
                {
                    player.Room = this;
                }
                foreach(Player player in room.reconnectedPlayers)
                {
                    if(!GetPlayerById(player.playerId, out Player existingPlayer)) continue;

                    if(!ReconnectedPlayers.Contains(existingPlayer))
                        ReconnectedPlayers.Push(existingPlayer);
                }
            }
            else
            {
                throw new Exception("Failed to create room.");
            }
        }

        /// <summary>
        /// Gets a player by their ID.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="player"></param>
        /// <returns>if exists</returns>
        public bool GetPlayerById(string playerId, out Player player)
        {
            player = Players.FirstOrDefault(p => p.playerId == playerId);
            if (player != null)
                return true;
            return false;
        }

        /// <summary>
        /// Gets a player by their name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="player"></param>
        /// <returns>if exists</returns>
        public bool GetPlayerByName(string name, out Player player)
        {
            player = Players.FirstOrDefault(p => p.playerName == name);
            if (player != null)
                return true;
            return false;
        }

        /// <summary>
        /// Allow new players to join?
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SetLocked(bool state)
        {
            if (string.IsNullOrEmpty(SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            HttpContent content = new StringContent(state.ToString().ToLower(), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(LeeboxManager.Address + "/" + ID + "/setlocked", content);
            response.EnsureSuccessStatusCode();

            await SyncRoomData(); // Refresh room data after setting locked state
        }

        /// <summary>
        /// Broadcasts a message to all players in the room.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task Broadcast(string message)
        {
            if (string.IsNullOrEmpty(SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            string json = JsonSerializer.Serialize(message);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(LeeboxManager.Address + "/" + ID + "/broadcast", content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Sets the header image to a URL.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SetImage(string url)
        {
            if (string.IsNullOrEmpty(SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            string json = JsonSerializer.Serialize(url);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(LeeboxManager.Address + "/" + ID + "/setImage", content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Asks all players a question and waits for their responses.
        /// </summary>
        /// <param name="question"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Dictionary<Player, string>> AskAll(string question, int timeoutSeconds = 30)
        {
            if (string.IsNullOrEmpty(SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            string json = JsonSerializer.Serialize(question);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await client.PostAsync(LeeboxManager.Address + "/" + ID + "/ask"+ "?timeoutSeconds="+ timeoutSeconds, content);
            response.EnsureSuccessStatusCode();


            Dictionary<string,string> responses = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            Dictionary<Player, string> presponses = new Dictionary<Player, string>();
            foreach (var kvp in responses)
            {
                Player player = Players.FirstOrDefault(p => p.playerId == kvp.Key);
                if (player != null)
                {
                    presponses[player] = kvp.Value;
                }
            }

            return presponses;
        }

        public class OptionRequest
        {
            public string message { get; set; }
            public string[] options { get; set; }
            public string[] images { get; set; }

            public OptionRequest(string message, string[] options, string[] images)
            {
                this.message = message;
                this.options = options;
                this.images = images;
            }
        }

        /// <summary>
        /// Asks all players a question with multiple options and waits for their responses.
        /// </summary>
        /// <param name="question"></param>
        /// <param name="options"></param>
        /// <param name="images"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Dictionary<Player, string>> OptionAll(string question, string[] options, string[] images, int timeoutSeconds = 30)
        {
            if (string.IsNullOrEmpty(SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            OptionRequest request = new OptionRequest(question, options, images);

            string json = JsonSerializer.Serialize(request);

            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(LeeboxManager.Address + "/" + ID + "/options" + "?timeoutSeconds=" + timeoutSeconds, content);
            response.EnsureSuccessStatusCode();


            Dictionary<string, string> responses = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            Dictionary<Player, string> presponses = new Dictionary<Player, string>();
            foreach (var kvp in responses)
            {
                Player player = Players.FirstOrDefault(p => p.playerId == kvp.Key);
                if (player != null)
                {
                    presponses[player] = kvp.Value;
                }
            }

            return presponses;
        }

        /// <summary>
        /// Asks all players to draw something based on a prompt and waits for their responses.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Dictionary<Player, string>> DrawAll(string prompt, int timeoutSeconds = 30)
        {
            if (string.IsNullOrEmpty(SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            string json = JsonSerializer.Serialize(prompt);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(LeeboxManager.Address + "/" + ID + "/draw" + "?timeoutSeconds=" + timeoutSeconds, content);
            response.EnsureSuccessStatusCode();


            Dictionary<string, string> responses = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            Dictionary<Player, string> presponses = new Dictionary<Player, string>();
            foreach (var kvp in responses)
            {
                Player player = Players.FirstOrDefault(p => p.playerId == kvp.Key);
                if (player != null)
                {
                    presponses[player] = kvp.Value;
                }
            }

            return presponses;
        }

        public IEnumerator<Player> GetEnumerator()
        {
            foreach (Player player in Players)
            {
                yield return player;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
