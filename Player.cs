using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static LeeboxLib.Room;

namespace LeeboxLib
{
    public class Player
    {
        public string playerId { get; set; }
        public string playerName { get; set; }

        [JsonIgnore]
        public Room Room { get; set; }

        [Obsolete("This constructor is for deserialization only.")]
        public Player(string playerId, string playerName)
        {
            this.playerId = playerId;
            this.playerName = playerName;
        }
        /// <summary>
        /// Says a message to a specific player.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task Say(string message)
        {
            if (string.IsNullOrEmpty(Room.SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            HttpContent content = new StringContent($"\"{message}\"", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Room.client.PostAsync(LeeboxManager.Address + "/" + Room.ID + "/say/" + playerId, content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Sets the image for the player.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SetImage(string url)
        {
            if (string.IsNullOrEmpty(Room.SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            HttpContent content = new StringContent($"\"{url}\"", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Room.client.PostAsync(LeeboxManager.Address + "/" + Room.ID + "/setImage/" + playerId, content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Asks a question to the player and waits for a response.
        /// </summary>
        /// <param name="question"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> Ask(string question, int timeoutSeconds = 30)
        {
            if (string.IsNullOrEmpty(Room.SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            HttpContent content = new StringContent($"\"{question}\"", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Room.client.PostAsync(LeeboxManager.Address + "/" + Room.ID + "/ask/" + playerId + "?timeoutSeconds=" + timeoutSeconds, content);
            response.EnsureSuccessStatusCode();

            string answer = await response.Content.ReadAsStringAsync();
            return answer; // Remove quotes from the response
        }

        /// <summary>
        /// Asks a question to the player with multiple options and images, and waits for a response.
        /// </summary>
        /// <param name="question"></param>
        /// <param name="options"></param>
        /// <param name="images"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> Option(string question, string[] options, string[] images, int timeoutSeconds = 30)
        {
            if (string.IsNullOrEmpty(Room.SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            OptionRequest request = new OptionRequest(question, options, images);

            string json = JsonSerializer.Serialize(request);

            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Room.client.PostAsync(LeeboxManager.Address + "/" + Room.ID + "/options/" + playerId + "?timeoutSeconds=" + timeoutSeconds, content);
            response.EnsureSuccessStatusCode();

            string answer = await response.Content.ReadAsStringAsync();
            return answer;
        }

        /// <summary>
        /// Ask the player to draw something based on a prompt and wait for a response.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> Draw(string prompt, int timeoutSeconds = 30)
        {
            if (string.IsNullOrEmpty(Room.SecretKey))
                throw new InvalidOperationException("Room secret key is not set. Call Setup() first.");

            HttpContent content = new StringContent($"\"{prompt}\"", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Room.client.PostAsync(LeeboxManager.Address + "/" + Room.ID + "/draw/" + playerId + "?timeoutSeconds=" + timeoutSeconds, content);
            response.EnsureSuccessStatusCode();

            string answer = await response.Content.ReadAsStringAsync();
            return answer;
        }
    }
}
