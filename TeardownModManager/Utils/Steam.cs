﻿using Newtonsoft.Json;
using Steam.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TeardownModManager;

namespace Steam.Classes
{
    public class Cache
    {
        public List<CacheFileDetail> FileDetails = new List<CacheFileDetail>();
    }

    public class CacheFileDetail : Publishedfiledetail
    {
        public DateTime lastFetched { get; set; }

        public static CacheFileDetail FromPublishedfiledetail(Publishedfiledetail publishedfiledetail)
        {
            var fdstr = JsonConvert.SerializeObject(publishedfiledetail);
            var res = JsonConvert.DeserializeObject<CacheFileDetail>(fdstr);
            res.lastFetched = DateTime.Now;
            return res;
        }
    }
}

namespace Steam
{
    public static class Utils
    {
        private static FileInfo cacheFile = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).CombineFile("steam.cache.json");
        private static Cache cache;

        public static async Task<GetPublishedFileDetailsResponse> GetPublishedFileDetailsAsync(HttpClient webClient, Teardown.Mod Mod) => await GetPublishedFileDetailsAsync(webClient, Mod.SteamWorkshopId);

        public static async Task<GetPublishedFileDetailsResponse> GetPublishedFileDetailsAsync(HttpClient webClient, string fileId) => await GetPublishedFileDetailsAsync(webClient, new List<string>() { fileId });

        public static async Task<GetPublishedFileDetailsResponse> GetPublishedFileDetailsAsync(HttpClient webClient, List<string> fileIds)
        {
            fileIds.RemoveAll(id => string.IsNullOrWhiteSpace(id));
            fileIds.Remove("0");
            var parsedResponse = new GetPublishedFileDetailsResponse();
            if (fileIds.Count < 1) return parsedResponse;
            CheckCache();

            if (cacheFile.Exists && (!cacheFile.LastWriteTime.ExpiredSince(10)))
            {
                foreach (var fileId in fileIds)
                {
                    var item = cache.FileDetails.FirstOrDefault(x => x.publishedfileid == fileId);
                    if (item != null) parsedResponse.response.publishedfiledetails.Add(item);
                }

                if (parsedResponse.response.publishedfiledetails.Count >= fileIds.Count)
                    return parsedResponse;
            }

            /*SteamRequest request = new SteamRequest("ISteamRemoteStorage/GetPublishedFileDetails/v1/");
            request.AddParameter("itemcount", fileIds.Count);
            request.AddParameter("publishedfileids", fileIds.ToArray());
			var response = steam.Execute(request);
            Console.WriteLine(response.Content);
            */
            var values = new Dictionary<string, string> { { "itemcount", fileIds.Count.ToString() } };

            for (int i = 0; i < fileIds.Count; i++)
                values.Add($"publishedfileids[{i}]", fileIds[i].ToString());

            var content = new FormUrlEncodedContent(values);
            var url = new Uri("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/");
            Console.WriteLine($"[Steam] POST to {url} with payload {content.ToJson(false)} and values {values.ToJson(false)}");
            var response = await webClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            try { parsedResponse = JsonConvert.DeserializeObject<GetPublishedFileDetailsResponse>(responseString); }
            catch (Exception ex) { Console.WriteLine($"[Steam] Could not deserialize response ({ex.Message})\n{responseString}"); } // {response.ReasonPhrase} ({response.StatusCode})\n

            if (parsedResponse != null)
            {
                foreach (var item in parsedResponse.response.publishedfiledetails)
                {
                    cache.FileDetails.RemoveAll(x => x.publishedfileid == item.publishedfileid);
                    cache.FileDetails.Add(CacheFileDetail.FromPublishedfiledetail(item));
                }
            }

            File.WriteAllText(cacheFile.FullName, JsonConvert.SerializeObject(cache));
            return parsedResponse;
        }

        private static void CheckCache()
        {
            if (cache is null)
            {
                if (cacheFile.Exists)
                {
                    try
                    {
                        cache = JsonConvert.DeserializeObject<Cache>(File.ReadAllText(cacheFile.FullName));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] [Steam] Unable to load cache ({ex.Message}), starting over...");
                        cache = new Cache();
                    }
                }
                else
                {
                    cache = new Cache();
                }
            }
        }
    }
}