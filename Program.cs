using System;
using System.Net;
using System.IO;

namespace bilibilisubers
{
    class Program
    {
        private static string followersbaseUrl = "https://api.bilibili.com/x/relation/followers?vmid={0}";

        private static long vmId = 15834498;

        private static string nameBaseUrl = "https://api.bilibili.com/x/space/acc/info?mid={0}";

        private static int delaySec = 10000;

        private static bool isLoop = false;

        private static bool isVerbose = false;

        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--h" || args[0] == "-help" || args[0] == "--help")
            {
                System.Console.WriteLine("Usage: bilibilisubers [userId]");
                System.Console.WriteLine("       --help: Help");
                System.Console.WriteLine("       -o [millsecond]: Loop mode");
                System.Console.WriteLine("\nGet fowllower number of a specific user");
                return;
            }
            if (!long.TryParse(args[0], out vmId))
            {
                System.Console.WriteLine("userId must be number!");
                return;
            }
            if (args.Length > 1)
            {
                bool isExpectFlagParam = false;
                for (int i = 1; i < args.Length; i++)
                {
                    if (isExpectFlagParam)
                    {
                        DecodeFlags(args[i - 1], args[i].StartsWith("-") ? null : args[i]);
                        isExpectFlagParam = false;
                    }
                    if (args[i].StartsWith("-"))
                    {
                        isExpectFlagParam = true;
                        if (args.Length == i + 1)
                        {
                            DecodeFlags(args[i]);
                            isExpectFlagParam = false;
                        }
                    }
                    else
                    {
                        isExpectFlagParam = false;
                    }
                }
            }

            delaySec = delaySec > 10000 ? delaySec : 10000;

            if (isVerbose)
            {
                System.Console.WriteLine("[verbose]间隔最小10000");
                System.Console.WriteLine("[verbose]当前间隔{0}", delaySec);
                System.Console.WriteLine("[verbose]当前模式：{0}", isLoop ? "循环模式" : "查询模式");
            }

            if (isLoop)
            {
                NormalMode();
                LoopMode();
            }
            else
            {
                NormalMode();
            }
        }

        private static void DecodeFlags(string flag, string flagParam = null)
        {
            switch (flag)
            {
                case "-o":
                    isLoop = true;
                    if (flagParam == null)
                    {
                        return;
                    }
                    else if (!int.TryParse(flagParam, out delaySec))
                    {
                        System.Console.WriteLine("delay must be number");
                        Environment.Exit(-1);
                    }
                    break;
                case "-v":
                    isVerbose = true;
                    break;
            }
        }

        private static void NormalMode()
        {
            var followersobj = GetResponse<BiliBase<BiliFollowersData>>(followersbaseUrl, vmId);
            var upInfoObj = GetResponse<BiliBase<BiliInfoData>>(nameBaseUrl, vmId);
            if (followersobj == null && upInfoObj == null)
            {
                System.Console.WriteLine("未获取到数据");
                return;
            }
            else if (followersobj.data == null || upInfoObj.data == null)
            {
                if (followersobj.data == null)
                {
                    System.Console.WriteLine(followersobj.message);
                }
                if (upInfoObj.data == null)
                {
                    System.Console.WriteLine(upInfoObj.message);
                }
                return;
            }

            System.Console.WriteLine("Up主: {0}, \n他的签名： {1}, \n性别： {2}, \n他的脸： {3}, \n他的生日： {4}, \n是否认证： {5}, \n认证介绍： {6}, \n等级： {7}, \n排名: {8}, \n目前订阅人数： {9}",
            upInfoObj.data.name,
            upInfoObj.data.sign,
            upInfoObj.data.sex,
            upInfoObj.data.face,
            upInfoObj.data.birthday,
            upInfoObj.data.official.role,
            upInfoObj.data.official.title,
            upInfoObj.data.level,
            upInfoObj.data.rank,
            followersobj.data.total);
        }

        private static void LoopMode()
        {
            var name = "";
            var upInfoObj = GetResponse<BiliBase<BiliInfoData>>(nameBaseUrl, vmId);
            if (upInfoObj != null && upInfoObj.data != null)
            {
                name = upInfoObj.data.name;
            }
            else
            {
                System.Console.WriteLine("获取up主失败, 5s后重试...");
                System.Threading.Thread.Sleep(5000);
                LoopMode();
            }
            while (true)
            {
                var followersobj = GetResponse<BiliBase<BiliFollowersData>>(followersbaseUrl, vmId);
                if (followersobj != null && followersobj.data != null)
                {
                    System.Console.WriteLine("{0}当前关注数: {1}", name, followersobj.data.total);
                }
                else
                {
                    System.Console.WriteLine("获取失败");
                }
                System.Threading.Thread.Sleep(delaySec);
            }
        }

        private static T GetResponse<T>(string url, long userId)
        {
            try
            {
                WebRequest request = WebRequest.Create(string.Format(url, vmId));
                request.Method = "GET";
                request.Headers.Add("Referer", "https://space.bilibili.com/35836017/fans/fans");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.100 Safari/537.36");
                var response = request.GetResponse();
                if ((response as HttpWebResponse).StatusCode == HttpStatusCode.OK)
                {
                    var json = "";
                    var stream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        json = sr.ReadToEnd();
                    }
                    var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                    return obj;
                }
                else
                {
                    return default(T);
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }

    }

    class BiliBase<T>
    {
        public int code { get; set; }

        public string message { get; set; }

        public int ttl { get; set; }

        public T data { get; set; }
    }

    class BiliFollowersData
    {
        public object list { get; set; }

        public long re_version { get; set; }

        public long total { get; set; }
    }

    class BiliInfoData
    {
        public long mid { get; set; }

        public string name { get; set; }

        public string sex { get; set; }

        public string face { get; set; }

        public string sign { get; set; }

        public int rank { get; set; }

        public int level { get; set; }

        public long jointime { get; set; }

        public int moral { get; set; }

        public int silence { get; set; }

        public string birthday { get; set; }

        public long coins { get; set; }

        public bool fans_badge { get; set; }

        public Official official { get; set; }

        public Vip vip { get; set; }

        public bool is_followed { get; set; }

        public string top_photo { get; set; }

        public object theme { get; set; }

        public object sys_notice { get; set; }
    }

    class Official
    {
        public int role { get; set; }

        public string title { get; set; }

        public string desc { get; set; }
    }

    class Vip
    {
        public int type { get; set; }

        public int status { get; set; }

        public int theme_type { get; set; }
    }
}
