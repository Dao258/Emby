﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Implementations.Updates
{
    public class GithubUpdater
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private TimeSpan _cacheLength;

        public GithubUpdater(IHttpClient httpClient, IJsonSerializer jsonSerializer, TimeSpan cacheLength)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _cacheLength = cacheLength;
        }

        public async Task<CheckForUpdateResult> CheckForUpdateResult(string organzation, string repository, Version minVersion, bool includePrerelease, string assetFilename, string packageName, string targetFilename, CancellationToken cancellationToken)
        {
            var url = string.Format("https://api.github.com/repos/{0}/{1}/releases", organzation, repository);

            var options = new HttpRequestOptions
            {
                Url = url,
                EnableKeepAlive = false,
                CancellationToken = cancellationToken,
                UserAgent = "Emby/3.0"

            };

            if (_cacheLength.Ticks > 0)
            {
                options.CacheMode = CacheMode.Unconditional;
                options.CacheLength = _cacheLength;
            }

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                EnableKeepAlive = false,
                CancellationToken = cancellationToken,
                UserAgent = "Emby/3.0"

            }).ConfigureAwait(false))
            {
                var obj = _jsonSerializer.DeserializeFromStream<RootObject[]>(stream);

                var availableUpdate = CheckForUpdateResult(obj, minVersion, includePrerelease, assetFilename, packageName, targetFilename);

                return availableUpdate ?? new CheckForUpdateResult
                {
                    IsUpdateAvailable = false
                };
            }
        }

        private CheckForUpdateResult CheckForUpdateResult(RootObject[] obj, Version minVersion, bool includePrerelease, string assetFilename, string packageName, string targetFilename)
        {
            if (!includePrerelease)
            {
                obj = obj.Where(i => !i.prerelease).ToArray();
            }

            // TODO:
            // Filter using name and check for suffixes such as -beta, -dev?

            return obj.Select(i => CheckForUpdateResult(i, minVersion, assetFilename, packageName, targetFilename)).FirstOrDefault(i => i != null);
        }

        private CheckForUpdateResult CheckForUpdateResult(RootObject obj, Version minVersion, string assetFilename, string packageName, string targetFilename)
        {
            Version version;
            if (!Version.TryParse(obj.tag_name, out version))
            {
                return null;
            }

            if (version < minVersion)
            {
                return null;
            }

            var asset = (obj.assets ?? new List<Asset>()).FirstOrDefault(i => string.Equals(assetFilename, Path.GetFileName(i.browser_download_url), StringComparison.OrdinalIgnoreCase));

            if (asset == null)
            {
                return null;
            }

            return new CheckForUpdateResult
            {
                AvailableVersion = version.ToString(),
                IsUpdateAvailable = version > minVersion,
                Package = new PackageVersionInfo
                {
                    classification = obj.prerelease ? PackageVersionClass.Beta : PackageVersionClass.Release,
                    name = packageName,
                    sourceUrl = asset.browser_download_url,
                    targetFilename = targetFilename,
                    versionStr = version.ToString(),
                    requiredVersionStr = "1.0.0",
                    description = obj.body
                }
            };
        }

        public class Uploader
        {
            public string login { get; set; }
            public int id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        public class Asset
        {
            public string url { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public object label { get; set; }
            public Uploader uploader { get; set; }
            public string content_type { get; set; }
            public string state { get; set; }
            public int size { get; set; }
            public int download_count { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public string browser_download_url { get; set; }
        }

        public class Author
        {
            public string login { get; set; }
            public int id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        public class RootObject
        {
            public string url { get; set; }
            public string assets_url { get; set; }
            public string upload_url { get; set; }
            public string html_url { get; set; }
            public int id { get; set; }
            public string tag_name { get; set; }
            public string target_commitish { get; set; }
            public string name { get; set; }
            public bool draft { get; set; }
            public Author author { get; set; }
            public bool prerelease { get; set; }
            public string created_at { get; set; }
            public string published_at { get; set; }
            public List<Asset> assets { get; set; }
            public string tarball_url { get; set; }
            public string zipball_url { get; set; }
            public string body { get; set; }
        }
    }
}
