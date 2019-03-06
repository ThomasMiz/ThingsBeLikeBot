using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ThingsBeLikeBot
{
    class Program
    {
        const int POSTS_PER_DAY = 3;
        const int DELAY_BETWEEN_POSTS_MINUTES = 3 * 60;
        const int TIMER_START_SPAN = 2 * 60 * 1000; //2 minutes
        const int TIMER_PERIOD_SPAN = 30 * 60 * 1000; //30 minutes

        static String DATA_FOLDER;
        static String DATA_FILE;

        static MenuItem stopItem, showItem, postItem;
        static NotifyIcon icon;

        static Process process;

        static int timesPostedToday;
        static DateTime lastPost;

        static System.Threading.Timer timer;

        static void Main(string[] args)
        {
            DATA_FOLDER = Environment.GetEnvironmentVariable("APPDATA") + "/ThingsBeLikeBot";
            DATA_FILE = DATA_FOLDER + "/botdata";

            System.ComponentModel.IContainer container = new System.ComponentModel.Container();

            postItem = new MenuItem();
            postItem.Index = 0;
            postItem.Text = "Force post now";

            showItem = new MenuItem();
            showItem.Index = 1;
            showItem.Text = "Show posting data";

            stopItem = new MenuItem();
            stopItem.Index = 2;
            stopItem.Text = "Stop ThingsBeLikeBot";

            ContextMenu contextMenu = new ContextMenu(new MenuItem[] { postItem, showItem, stopItem });

            icon = new NotifyIcon(container);
            icon.ContextMenu = contextMenu;

            icon.Icon = new System.Drawing.Icon(DATA_FOLDER + "/ico.ico");
            icon.Visible = true;
            icon.Text = "haha yes";

            postItem.Click += PostItem_Click;
            showItem.Click += ShowItem_Click;
            stopItem.Click += MenuItem_Click;

            timer = new System.Threading.Timer(new System.Threading.TimerCallback(Timercall), null, TIMER_START_SPAN, TIMER_PERIOD_SPAN);
            Application.Run();

            showItem.Dispose();
            postItem.Dispose();
            stopItem.Dispose();
            icon.Icon.Dispose();
            icon.Dispose();
            contextMenu.Dispose();
            timer.Dispose();
        }

        private static void ShowItem_Click(object sender, EventArgs e)
        {
            String title, tip;
            ReadBotData(out timesPostedToday, out lastPost);
            if (DateTime.Now.Day != lastPost.Day)
                timesPostedToday = 0;

            if (timesPostedToday == POSTS_PER_DAY)
            {
                tip = "Come back tomorrow";
                title = "Done posting for today.";
            }
            else
            {
                title = String.Concat("Posted today: ", timesPostedToday, "/", POSTS_PER_DAY, ".");
                if (timesPostedToday < POSTS_PER_DAY)
                {
                    StringBuilder builder = new StringBuilder(30);
                    TimeSpan diff = DateTime.Now.Subtract(lastPost.AddMinutes(DELAY_BETWEEN_POSTS_MINUTES));
                    builder.Append("Last posted at ");
                    builder.Append(lastPost.Hour);
                    builder.Append(':');
                    builder.Append(lastPost.Minute);
                    builder.AppendLine(".");
                    builder.Append("Next post in ");
                    if (diff.Hours != 0)
                    {
                        builder.Append(-diff.Hours);
                        builder.Append("h ");
                    }
                    builder.Append(-diff.Minutes);
                    builder.Append("m");
                    tip = builder.ToString();
                }
                else
                    tip = "Not posting again.";
            }
            icon.ShowBalloonTip(6000, title, tip, ToolTipIcon.Info);
        }

        private static void PostItem_Click(object sender, EventArgs e)
        {
            PostAnotherMeme();
        }

        private static void Timercall(object a)
        {
            DateTime now = DateTime.Now;
            ReadBotData(out timesPostedToday, out lastPost);
            if (lastPost.Day != now.Day)
            { //if it's a new day
                timesPostedToday = 0;
            }

            if(timesPostedToday < POSTS_PER_DAY && process == null && now.Subtract(lastPost).TotalMinutes >= DELAY_BETWEEN_POSTS_MINUTES)
            {
                PostAnotherMeme();
            }
        }

        /// <summary>
        /// Posts another meme. Doesn't check if the previous process is done, or if it's a nice time to do so. It just GOES.
        /// The process exit event does check if the process succeeded tho
        /// </summary>
        private static void PostAnotherMeme()
        {
            process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo = new ProcessStartInfo(DATA_FOLDER + "/ImageCreator.exe")
            {
                CreateNoWindow = true,
                WorkingDirectory = Path.GetFullPath(DATA_FOLDER)
            };
            process.Exited += P_Exited;
            process.Start();
        }

        private static void P_Exited(object sender, EventArgs e)
        {
            if (process.ExitCode == 0)
            {
                timesPostedToday++;
                lastPost = DateTime.Now;
                WriteBotData(timesPostedToday, lastPost);

                icon.ShowBalloonTip(5000, "Posted.", "posts today: " + timesPostedToday, ToolTipIcon.None);
            }
            else if(process.ExitCode != -1) //ExitCode of -1 means it couldn't connect to the internet
                icon.ShowBalloonTip(5000, "Seems like something went wrong on ImageCreator", "The process has terminated with code " + process.ExitCode, ToolTipIcon.Error);

            process.Dispose();
            process = null;
        }

        private static void MenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("Stop ThingsBeLikeBot?", "ThingsBeLikeBot", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
                Application.Exit();
        }

        /// <summary>
        /// Writes the bot posting information to a file (creating or overwritting it)
        /// </summary>
        private static void WriteBotData(int timesPosted, DateTime time)
        {
            File.WriteAllText(DATA_FILE, String.Concat(timesPosted, ",", time.Year, ",", time.Month, ",", time.Day, ",", time.Hour, ",", time.Minute, ",", time.Second, ",", time.Millisecond));
        }

        /// <summary>
        /// Reads the bot posting information from the file. If the file doesn't exist, returns default values
        /// </summary>
        private static void ReadBotData(out int timesPosted, out DateTime time)
        {
            if (File.Exists(DATA_FILE))
            {
                String[] data = File.ReadAllText(DATA_FILE).Split(',');
                if (data.Length == 8)
                {
                    int[] d = new int[7];
                    if (Int32.TryParse(data[0], out timesPosted) && Int32.TryParse(data[1], out d[0]) && Int32.TryParse(data[2], out d[1]) && Int32.TryParse(data[3], out d[2])
                        && Int32.TryParse(data[4], out d[3]) && Int32.TryParse(data[5], out d[4]) && Int32.TryParse(data[6], out d[5]) && Int32.TryParse(data[7], out d[6]))
                    {
                        time = new DateTime(d[0], d[1], d[2], d[3], d[4], d[5], d[6]);
                        return;
                    }
                }
            }

            timesPosted = 0;
            time = DateTime.MinValue;
        }
    }
}
