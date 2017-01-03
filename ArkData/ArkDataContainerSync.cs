using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ArkData
{
    /// <summary>
    /// The container for the ARK data.
    /// </summary>
    public partial class ArkDataContainer
    {
        /// <summary>
        /// Loads the profile data for all users from the steam service
        /// </summary>
        /// <param name="apiKey">The Steam API key</param>
        public void LoadSteam(string apiKey)
        {
            //FIX FOR STEAM API RETURNS ERROR ON BULK STEAMIDS OVER 430 IDS
            //NOW REQUEST 100 STEAMIDS AT A TIME
            double d_antPlayers = Players.Count;
            double antLoops = System.Math.Ceiling(d_antPlayers / 100.0);
            int antPlayers = Players.Count;
            int posTeller = 0;

            for (var ant = 1; ant <= antLoops; ant++)
            {
                var range = 0;
                if (ant == antLoops)
                {
                    range = antPlayers - ((ant - 1) * 100);
                }
                else
                {
                    range = 100;
                }

                var builder = new StringBuilder();
                for (var i = 0; i < range; i++)
                {
                    builder.Append(Players[posTeller].SteamId + ",");
                    posTeller++;
                }

                //System.Windows.Forms.MessageBox.Show("Range: " + range + "\n" + builder.ToString());
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("https://api.steampowered.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = client.GetAsync(
                        $"ISteamUser/GetPlayerSummaries/v0002/?key={apiKey}&steamids={builder.ToString()}").Result;
                    if (response.IsSuccessStatusCode)
                        using (var reader = new StreamReader(response.Content.ReadAsStreamAsync().Result))
                        {
                            LinkSteamProfiles(reader.ReadToEnd());
                        }
                    else
                        throw new System.Net.WebException("(LinkSteamProfiles(" + Players.Count + " players))The Steam API request was unsuccessful. Are you using a valid key?");

                    response = client.GetAsync(
                        $"ISteamUser/GetPlayerBans/v1/?key={apiKey}&steamids={builder.ToString()}").Result;
                    if (response.IsSuccessStatusCode)
                        using (var reader = new StreamReader(response.Content.ReadAsStreamAsync().Result))
                        {
                            LinkSteamBans(reader.ReadToEnd());
                        }
                    else
                        throw new System.Net.WebException("(LinkSteamBans)The Steam API request was unsuccessful. Are you using a valid key?");
                }
            }
            SteamLoaded = true;
        }

        /// <summary>
        /// Fetches the player server status. Can only be done after fetching Steam player data.
        /// </summary>
        /// <param name="ipString">The IP of the server.</param>
        /// <param name="port">The port of the server.</param>
        public void LoadOnlinePlayers(string ipString, int port)
        {
            if (SteamLoaded)
            {
                LinkOnlinePlayers(ipString, port);
            }
            else
                throw new System.Exception("The Steam user data should be loaded before the server status can be checked.");
        }

        /// <summary>
        /// Instantiates the ArkDataContainer and parses all the user data files
        /// </summary>
        /// <param name="directory">The directory containing the profile and tribe files.</param>
        public static ArkDataContainer Create(string directory)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException("The ARK data directory couldn't be found.");

            var playerFiles = Directory.GetFiles(directory).Where(p => p.ToLower().Contains(".arkprofile")).ToArray();
            var tribeFiles = Directory.GetFiles(directory).Where(p => p.ToLower().Contains(".arktribe")).ToArray();

            if (playerFiles.Length == 0 && tribeFiles.Length == 0)
                throw new FileLoadException("The directory did not contain any of the parseable files.");

            var container = new ArkDataContainer();

            for (var i = 0; i < playerFiles.Length; i++)
                container.Players.Add(Parser.ParsePlayer(playerFiles[i]));

            for (var i = 0; i < tribeFiles.Length; i++)
                container.Tribes.Add(Parser.ParseTribe(tribeFiles[i]));

            container.LinkPlayerTribe();

            return container;
        }
    }
}
