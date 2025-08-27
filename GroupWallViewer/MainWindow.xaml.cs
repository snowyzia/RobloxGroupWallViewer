using GroupWallViewer.View.UserControls;
using GroupWallViewer.View.Windows;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GroupWallViewer
{
    public partial class MainWindow : Window
    {
        private BitmapImage defaultImage = new BitmapImage(new Uri("pack://application:,,,/PlaceholderImages/MissingIcon.jpg"));
        private List<WallPostData> wallPostData = new List<WallPostData>();
        private int currentPage = 0;
        private readonly int messagesPerPage = 50;

        public MainWindow()
        {
            InitializeComponent();
        }

        // buttons
        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void ClearCache(object sender, RoutedEventArgs e) // if we want to clear cache and stuff idk....
        {
            MessageBoxResult box = MessageBox.Show("This feature has been disabled, I do NOT trust myself enough. Please manually delete the UserPictures and GroupPictures folders to clear the icon cache.", "Clear Icon Cache", MessageBoxButton.OK, MessageBoxImage.Information);
            //MessageBoxResult box = MessageBox.Show("Are you sure you want to clear the icon cache?", "Clear Icon Cache", MessageBoxButton.YesNo, MessageBoxImage.Question);
            //if (box == MessageBoxResult.Yes)
            //{
            //    string? directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //    string userPicturesPath = Path.Combine(directoryPath, "UserPictures");
            //    Directory.CreateDirectory(userPicturesPath);

            //    string groupPicturesPath = Path.Combine(directoryPath, "GroupPictures");
            //    Directory.CreateDirectory(groupPicturesPath);

            //    foreach (var userPicture in Directory.EnumerateFiles(userPicturesPath))
            //    {
            //        string extension = Path.GetExtension(userPicture);
            //        if (Path.GetFileNameWithoutExtension(userPicture).All(char.IsDigit) & extension == ".png") // ids are like 12345 blah blah so this is a double check yk
            //        {
            //            File.Delete(userPicture);
            //        }
            //    }

            //    foreach (var groupPicture in Directory.EnumerateFiles(groupPicturesPath))
            //    {
            //        string extension = Path.GetExtension(groupPicture);
            //        if (Path.GetFileNameWithoutExtension(groupPicture).All(char.IsDigit) & extension == ".png")
            //        {
            //            File.Delete(groupPicture);
            //        }
            //    }
            //}
        }
        private void CloseGroup(object sender, RoutedEventArgs e)
        {
            clearCache.Visibility = Visibility.Visible;
            controlGrid.Visibility = Visibility.Collapsed;
            infoText.Text = "Waiting on group data...";

            groupHolder.Children.Clear();
            wallPostName.Text = "";
            wallPostDescription.Text = "";
            wallPostIcon.Source = null;

            wallPostData.Clear();
            currentPage = 0;
        }
        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        // main things but one more button....t
        private void OpenJson(object sender, RoutedEventArgs e)
        {
            CloseGroup(sender, e);

            OpenFileDialog groupDataDialog = new OpenFileDialog();
            groupDataDialog.Filter = "Group Wall Data| *.json";
            groupDataDialog.Title = "Please select group data...";
            groupDataDialog.Multiselect = true;
            groupDataDialog.RestoreDirectory = true;

            if (groupDataDialog.ShowDialog() == true)
            {
                clearCache.Visibility = Visibility.Collapsed;
                try
                {
                    infoText.Text = "Group wall is loading, please wait!";
                    foreach (var file in groupDataDialog.FileNames)
                    {
                        using StreamReader groupDataReader = new StreamReader(file);
                        string groupDataJson = groupDataReader.ReadToEnd();

                        using JsonDocument groupDataDocument = JsonDocument.Parse(groupDataJson);
                        var groupData = groupDataDocument.RootElement.GetProperty("data");

                        foreach (JsonElement property in groupData.EnumerateArray())
                        {
                            var posterData = property.GetProperty("poster");
                            if (posterData.ValueKind != JsonValueKind.Null)
                            {
                                var userData = posterData.GetProperty("user");
                                var roleName = posterData.GetProperty("role").GetProperty("name");

                                var body = property.GetProperty("body");
                                var username = userData.GetProperty("username");
                                var displayName = userData.GetProperty("displayName");
                                var userId = userData.GetProperty("userId");

                                var created = property.GetProperty("created");
                                DateTimeOffset dto = DateTimeOffset.Parse(created.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime();
                                string formattedText = dto.ToString("MMMM dd, yyyy '|' hh:mm:ss tt", CultureInfo.InvariantCulture);

                                WallPostData postData = new WallPostData();
                                postData.WallText = body.ToString();
                                postData.Username = username.ToString();
                                postData.DisplayName = displayName.ToString();
                                postData.UserId = userId.ToString();
                                postData.AdditionalInformation = $"{roleName.ToString()} | {formattedText}";
                                wallPostData.Add(postData);
                            }
                        }
                    }

                    string? firstPath = Path.GetDirectoryName(groupDataDialog.FileNames[0].ToString());
                    string groupInfoPath = Path.Combine(firstPath, "group-info.json");
                    if (Path.Exists(groupInfoPath))
                    {
                        LoadGroupInfo(groupInfoPath);
                    } 
                    else
                    {
                        OpenFileDialog groupInfoDialog = new OpenFileDialog();
                        groupInfoDialog.Filter = "Group Wall Info| *.json";
                        groupInfoDialog.Title = "Please select group info...";
                        groupInfoDialog.RestoreDirectory = true;

                        if (groupInfoDialog.ShowDialog() == true)
                        {
                            LoadGroupInfo(groupInfoDialog.FileName);
                        }
                    }
                }

                catch (Exception ex)
                {
                    infoText.Text = $"Group Data has failed to load: {ex.ToString()}";
                }

                SwitchPage(0);
            }
        }
        private void PageTextEntered(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(pageText.Text, out int pageNumber))
                {
                    if (pageNumber > 0 & (pageNumber - 1) * messagesPerPage < wallPostData.Count)
                    {
                        SwitchPage(pageNumber - 1);
                    }
                }

                double totalPages = Math.Ceiling(wallPostData.Count / (double)messagesPerPage);
                pageText.Text = $"Page {currentPage + 1}/{totalPages}";
            }
        }
        private async void LoadGroupInfo(string filePath)
        {
            try
            {
                using StreamReader groupInfoReader = new StreamReader(filePath);
                string json = groupInfoReader.ReadToEnd();

                using JsonDocument groupInfoDocument = JsonDocument.Parse(json);

                var groupName = groupInfoDocument.RootElement.GetProperty("GroupName");
                var groupDescription = groupInfoDocument.RootElement.GetProperty("Description");
                var groupId = groupInfoDocument.RootElement.GetProperty("Id");
                wallPostName.Text = groupName.ToString();
                wallPostDescription.Text = groupDescription.ToString();

                BitmapImage groupPicture = await GetGroupPictureAsync(groupId.ToString());

                wallPostIcon.Source = groupPicture;

                infoText.Text = $"Fully loaded group: {groupName.ToString()}";
            }
            catch (Exception ex)
            {
                infoText.Text = $"Failed to load group information, group wall will still be shown: {ex.ToString()}";
            }
        }
        private async void SwitchPage(int pageNumber)
        {
            currentPage = pageNumber;


            if (currentPage > 0)
            {
                prevButton.Visibility = Visibility.Visible;
            }
            else
            {
                prevButton.Visibility = Visibility.Hidden;
            }

            if ((currentPage + 1) * messagesPerPage < wallPostData.Count)
            {
                nextButton.Visibility = Visibility.Visible;
            }
            else
            {
                nextButton.Visibility = Visibility.Hidden;
            }

            double totalPages = Math.Ceiling(wallPostData.Count / (double)messagesPerPage);
            pageText.Text = $"Page {currentPage + 1}/{totalPages}";

            if (pageNumber*messagesPerPage < wallPostData.Count)
            {
                groupHolder.Children.Clear();
                controlGrid.Visibility = Visibility.Visible;

                int startingIndex = pageNumber * messagesPerPage;
                int rangeCount = messagesPerPage;
                if (startingIndex+messagesPerPage > wallPostData.Count)
                {
                    rangeCount = wallPostData.Count-startingIndex;
                }

                List<WallPostData> data = wallPostData.GetRange(startingIndex, rangeCount);
                List<WallPost> wallPosts = new List<WallPost>();

                if (Properties.Settings.Default.DisplayUserIcons)
                {
                    // batch request icons so im not spamming tf outta the api
                    List<string> userIds = new List<string>();
                    foreach (var d in data)
                    {
                        string userId = d.UserId;
                        if (userIds.Count < 100 & !userIds.Contains(userId))
                        {
                            userIds.Add(userId);
                        }
                    }
                    BatchUserPictures(userIds);
                }

                foreach (var d in data)
                {
                    WallPost wallPost = new WallPost();

                    if (Properties.Settings.Default.DisplayUserIcons)
                    {
                        string userId = d.UserId;
                        BitmapImage userPicture = await GetUserPictureAsync(userId);
                        wallPost.playerIcon.Source = userPicture;
                    }
                    else
                    {
                        wallPost.playerIcon.Source = defaultImage;
                    }

                    wallPost.Username = $"{d.DisplayName} (@{d.Username})";
                    if (d.Username == d.DisplayName) {
                        wallPost.Username = d.Username;
                    }

                    wallPost.WallText = d.WallText;
                    wallPost.AdditionalInformation = d.AdditionalInformation;
                    wallPosts.Add(wallPost);
                }

                // it takes a while and i dont want the user wondering why half the group wall is not loaded
                foreach (WallPost wallPost in wallPosts)
                {
                    groupHolder.Children.Add(wallPost);
                }
            }
            else
            {
                CloseGroup(this, new RoutedEventArgs());
            }
        }
        private void PreviousPage(object sender, RoutedEventArgs e)
        {
            if (currentPage > 0)
            {
                SwitchPage(currentPage - 1);
            }
        }
        private void NextPage(object sender, RoutedEventArgs e)
        {
            if ((currentPage + 1) * messagesPerPage < wallPostData.Count)
            {
                SwitchPage(currentPage + 1);
            }
        }
        private async Task<BitmapImage> GetUserPictureAsync(string userId)
        {
            string? directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string newPath = Path.Combine(directoryPath, "UserPictures");
            Directory.CreateDirectory(newPath);

            string imagePath = Path.Combine(newPath, $"{userId}.png");
            return await GetPictureAsync($"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png&isCircular=false", newPath, imagePath);
        }
        private async Task<BitmapImage> GetGroupPictureAsync(string groupId)
        {
            string? directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string newPath = Path.Combine(directoryPath, "GroupPictures");
            Directory.CreateDirectory(newPath);

            string imagePath = Path.Combine(newPath, $"{groupId}.png");
            return await GetPictureAsync($"https://thumbnails.roblox.com/v1/groups/icons?groupIds={groupId}&size=150x150&format=Png&isCircular=false", newPath, imagePath);
        }
        private async Task<BitmapImage> GetPictureAsync(string url, string newPath, string imagePath)
        {
            if (Path.Exists(imagePath))
            {
                return LoadImage(imagePath);
            }
            else
            {
                try
                {
                    using HttpClient httpClient = new HttpClient();
                    var response = httpClient.GetStringAsync(url).Result;
                    using JsonDocument userHeadshotInfo = JsonDocument.Parse(response.ToString());
                    var data = userHeadshotInfo.RootElement.GetProperty("data")[0];

                    string targetId = data.GetProperty("targetId").ToString();
                    string state = data.GetProperty("state").ToString();
                    var imageUrl = data.GetProperty("imageUrl").ToString();
                    if (state == "Completed" & imageUrl != "")
                    {
                        byte[] imageBytes = await httpClient.GetByteArrayAsync(imageUrl.ToString());
                        await File.WriteAllBytesAsync(imagePath, imageBytes);
                        return LoadImage(imagePath);
                    }
                    else
                    {
                        return defaultImage;
                    }
                }
                catch
                {
                    return defaultImage;
                }
            }
        }
        private void BatchUserPictures(List<string> userIdList)
        {
            string? directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string newPath = Path.Combine(directoryPath, "UserPictures");
            Directory.CreateDirectory(newPath);

            string userIds = "";
            foreach (var userId in userIdList)
            {
                string imagePath = Path.Combine(newPath, $"{userId}.png");
                if (!Path.Exists(imagePath))
                {
                    if (userIds == "")
                    {
                        userIds = userId;
                    }
                    else
                    {
                        userIds = userIds + $",{userId}";
                    }
                }
            }
            BatchPicturesAsync($"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userIds}&size=150x150&format=Png&isCircular=false", newPath, userIds);
        }
        private async void BatchPicturesAsync(string url, string newPath, string userIds)
        {
            if (userIds == "")
            {
                return;
            }

            try
            {
                using HttpClient httpClient = new HttpClient();
                var response = httpClient.GetStringAsync(url).Result;
                using JsonDocument userHeadshotInfo = JsonDocument.Parse(response.ToString());
                var data = userHeadshotInfo.RootElement.GetProperty("data");

                string[] ids = userIds.Split(',');

                foreach (var item in data.EnumerateArray())
                {
                    string targetId = item.GetProperty("targetId").ToString();
                    string state = item.GetProperty("state").ToString();
                    var imageUrl = item.GetProperty("imageUrl").ToString();

                    if (state == "Completed" & imageUrl != "")
                    {
                        string imagePath = Path.Combine(newPath, $"{targetId}.png");
                        byte[] imageBytes = await httpClient.GetByteArrayAsync(imageUrl.ToString());
                        await File.WriteAllBytesAsync(imagePath, imageBytes); // automatically .png
                    }
                }
            }
            catch { }
        }
        private static BitmapImage LoadImage(string imagePath)
        {
            BitmapImage bitmapImage = null;
            if (imagePath != null)
            {
                BitmapImage image = new BitmapImage();
                using FileStream stream = File.OpenRead(imagePath);

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                bitmapImage = image;
            }
            return bitmapImage;
        }
    }

    public class WallPostData
    {
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string? UserId { get; set; }
        public string? WallText { get; set; }
        public string? AdditionalInformation { get; set; }
    }
}