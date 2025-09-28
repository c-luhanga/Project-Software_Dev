using UniShare.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace UniShare.Data
{
    public static class DevSeeder
    {
        public static async Task SeedAsync(UniShareDbContext db)
        {
            if (await db.Users.AnyAsync()) return;

            // Seed Users with different scenarios
            var password = "dummyhash";
            var users = new List<User>();
            
            // Create users with different profiles for testing
            var userProfiles = new[]
            {
                ("Alice", "Smith", "alice@principia.edu", false, false, "Dobson Hall"),
                ("Bob", "Jones", "bob@principia.edu", false, false, "Anderson Hall"),
                ("Carol", "Lee", "carol@principia.edu", false, false, "Johnson Hall"),
                ("David", "Wilson", "david@principia.edu", false, false, "Anderson Hall"),
                ("Emma", "Brown", "emma@principia.edu", false, false, "Dobson Hall"),
                ("Frank", "Davis", "frank@principia.edu", false, false, "Johnson Hall"),
                ("Grace", "Miller", "grace@principia.edu", false, false, null), // No house assigned
                ("Henry", "Garcia", "henry@principia.edu", false, false, "Anderson Hall"),
                ("Admin", "User", "admin@principia.edu", false, true, null), // Admin user
                ("Banned", "User", "banned@principia.edu", true, false, "Dobson Hall") // Banned user
            };

            foreach (var (first, last, email, isBanned, isAdmin, house) in userProfiles)
            {
                var salt = RandomNumberGenerator.GetBytes(16);
                var hash = HashPassword(password, salt);
                var user = new User
                {
                    FirstName = first,
                    LastName = last,
                    Email = email,
                    PasswordHash = hash,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)), // Random creation dates
                    IsBanned = isBanned,
                    IsAdmin = isAdmin,
                    Phone = Random.Shared.Next(0, 3) == 0 ? $"555-{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}" : null,
                    FirebaseUid = null,
                    House = house,
                    LastSeen = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 1440)), // Random last seen times
                    ProfileImageUrl = Random.Shared.Next(0, 3) == 0 ? $"https://example.com/avatars/{first.ToLower()}.jpg" : null
                };
                users.Add(user);
            }
            
            db.Users.AddRange(users);
            await db.SaveChangesAsync();

            // Seed Items with comprehensive test scenarios
            var categories = new[] { "Furniture", "Electronics", "Books", "Clothing", "Sports", "Kitchen", "Decor", "School Supplies" };
            var conditions = new[] { "New", "LikeNew", "Good", "Fair", "Poor" };
            
            var items = new List<Item>
            {
                // Free items for testing free filter
                new Item { Title = "Free Desk Lamp", Description = "Nice desk lamp, works perfectly", Category = "Furniture", Price = 0, Condition = "Good", SellerId = users[0].Id, PostedDate = DateTime.UtcNow.AddDays(-1) },
                new Item { Title = "Free Textbooks", Description = "Calculus and Physics textbooks", Category = "Books", Price = 0, Condition = "Fair", SellerId = users[1].Id, PostedDate = DateTime.UtcNow.AddDays(-2) },
                new Item { Title = "Free Coffee Mug", Description = "Principia College mug", Category = "Kitchen", Price = 0, Condition = "Good", SellerId = users[2].Id, PostedDate = DateTime.UtcNow.AddDays(-3) },
                
                // Low-priced items
                new Item { Title = "Used Notebooks", Description = "Pack of 5 spiral notebooks", Category = "School Supplies", Price = 5.00m, Condition = "Good", SellerId = users[0].Id, PostedDate = DateTime.UtcNow.AddDays(-1) },
                new Item { Title = "Pencil Case", Description = "Blue pencil case with zipper", Category = "School Supplies", Price = 3.50m, Condition = "LikeNew", SellerId = users[3].Id, PostedDate = DateTime.UtcNow.AddDays(-4) },
                new Item { Title = "Old T-Shirt", Description = "Vintage band t-shirt", Category = "Clothing", Price = 8.00m, Condition = "Good", SellerId = users[4].Id, PostedDate = DateTime.UtcNow.AddDays(-5) },
                
                // Mid-range items
                new Item { Title = "Study Desk", Description = "Wooden study desk with drawers", Category = "Furniture", Price = 45.00m, Condition = "Good", SellerId = users[1].Id, PostedDate = DateTime.UtcNow.AddDays(-2) },
                new Item { Title = "Bluetooth Speaker", Description = "Portable Bluetooth speaker, great sound", Category = "Electronics", Price = 35.00m, Condition = "LikeNew", SellerId = users[2].Id, PostedDate = DateTime.UtcNow.AddDays(-3) },
                new Item { Title = "Basketball", Description = "Official size basketball", Category = "Sports", Price = 15.00m, Condition = "Good", SellerId = users[5].Id, PostedDate = DateTime.UtcNow.AddDays(-6) },
                new Item { Title = "Winter Jacket", Description = "Warm winter jacket, size M", Category = "Clothing", Price = 25.00m, Condition = "Good", SellerId = users[6].Id, PostedDate = DateTime.UtcNow.AddDays(-7) },
                
                // Higher-priced items
                new Item { Title = "Mountain Bike", Description = "21-speed mountain bike, excellent condition", Category = "Sports", Price = 150.00m, Condition = "LikeNew", SellerId = users[3].Id, PostedDate = DateTime.UtcNow.AddDays(-4) },
                new Item { Title = "Gaming Monitor", Description = "24-inch gaming monitor, 144Hz", Category = "Electronics", Price = 200.00m, Condition = "New", SellerId = users[4].Id, PostedDate = DateTime.UtcNow.AddDays(-5) },
                new Item { Title = "Leather Sofa", Description = "Two-seater leather sofa", Category = "Furniture", Price = 300.00m, Condition = "Good", SellerId = users[7].Id, PostedDate = DateTime.UtcNow.AddDays(-8) },
                
                // Various conditions for testing
                new Item { Title = "Broken Calculator", Description = "Scientific calculator, needs repair", Category = "Electronics", Price = 5.00m, Condition = "Poor", SellerId = users[5].Id, PostedDate = DateTime.UtcNow.AddDays(-6) },
                new Item { Title = "Brand New Backpack", Description = "Never used hiking backpack", Category = "Sports", Price = 80.00m, Condition = "New", SellerId = users[6].Id, PostedDate = DateTime.UtcNow.AddDays(-7) },
                
                // Kitchen items
                new Item { Title = "Microwave", Description = "Compact microwave, perfect for dorms", Category = "Kitchen", Price = 60.00m, Condition = "Good", SellerId = users[7].Id, PostedDate = DateTime.UtcNow.AddDays(-8) },
                new Item { Title = "Plate Set", Description = "Set of 4 dinner plates", Category = "Kitchen", Price = 12.00m, Condition = "LikeNew", SellerId = users[0].Id, PostedDate = DateTime.UtcNow.AddDays(-1) },
                
                // Decor items
                new Item { Title = "Wall Art", Description = "Framed abstract art piece", Category = "Decor", Price = 20.00m, Condition = "Good", SellerId = users[1].Id, PostedDate = DateTime.UtcNow.AddDays(-2) },
                new Item { Title = "Table Lamp", Description = "Modern table lamp with adjustable arm", Category = "Decor", Price = 30.00m, Condition = "LikeNew", SellerId = users[2].Id, PostedDate = DateTime.UtcNow.AddDays(-3) },
                
                // Items from banned user (for testing admin functionality)
                new Item { Title = "Suspicious Item", Description = "This shouldn't be visible", Category = "Electronics", Price = 100.00m, Condition = "New", SellerId = users[9].Id, PostedDate = DateTime.UtcNow.AddDays(-1) }
            };
            
            db.Items.AddRange(items);
            await db.SaveChangesAsync();

            // Seed Item Images for some items
            var itemImages = new List<ItemImage>
            {
                new ItemImage { ItemId = items[6].Id, ImageUrl = "https://example.com/images/desk1.jpg" },
                new ItemImage { ItemId = items[6].Id, ImageUrl = "https://example.com/images/desk2.jpg" },
                new ItemImage { ItemId = items[7].Id, ImageUrl = "https://example.com/images/speaker.jpg" },
                new ItemImage { ItemId = items[10].Id, ImageUrl = "https://example.com/images/bike1.jpg" },
                new ItemImage { ItemId = items[10].Id, ImageUrl = "https://example.com/images/bike2.jpg" },
                new ItemImage { ItemId = items[10].Id, ImageUrl = "https://example.com/images/bike3.jpg" },
                new ItemImage { ItemId = items[11].Id, ImageUrl = "https://example.com/images/monitor.jpg" }
            };
            
            db.ItemImages.AddRange(itemImages);
            await db.SaveChangesAsync();

            // Seed multiple conversations for comprehensive testing
            var conversations = new List<Conversation>();
            
            // Conversation 1: Alice and Bob about the desk
            var conv1 = new Conversation
            {
                LastMessage = "Sure, I'll bring it to the lobby tomorrow at 3 PM.",
                LastUpdated = DateTime.UtcNow.AddMinutes(-30)
            };
            conversations.Add(conv1);
            
            // Conversation 2: Carol and David about the bike
            var conv2 = new Conversation
            {
                LastMessage = "Is the bike still available?",
                LastUpdated = DateTime.UtcNow.AddMinutes(-120)
            };
            conversations.Add(conv2);
            
            // Conversation 3: Emma and Frank about textbooks
            var conv3 = new Conversation
            {
                LastMessage = "Thanks for the quick response!",
                LastUpdated = DateTime.UtcNow.AddMinutes(-45)
            };
            conversations.Add(conv3);
            
            // Conversation 4: Grace and Henry about gaming monitor
            var conv4 = new Conversation
            {
                LastMessage = "What's your best price?",
                LastUpdated = DateTime.UtcNow.AddMinutes(-15)
            };
            conversations.Add(conv4);

            db.Conversations.AddRange(conversations);
            await db.SaveChangesAsync();

            // Seed conversation participants
            var participants = new List<ConversationParticipant>
            {
                // Conversation 1: Alice (0) and Bob (1)
                new ConversationParticipant { ConversationId = conversations[0].Id, UserId = users[0].Id },
                new ConversationParticipant { ConversationId = conversations[0].Id, UserId = users[1].Id },
                
                // Conversation 2: Carol (2) and David (3)
                new ConversationParticipant { ConversationId = conversations[1].Id, UserId = users[2].Id },
                new ConversationParticipant { ConversationId = conversations[1].Id, UserId = users[3].Id },
                
                // Conversation 3: Emma (4) and Frank (5)
                new ConversationParticipant { ConversationId = conversations[2].Id, UserId = users[4].Id },
                new ConversationParticipant { ConversationId = conversations[2].Id, UserId = users[5].Id },
                
                // Conversation 4: Grace (6) and Henry (7)
                new ConversationParticipant { ConversationId = conversations[3].Id, UserId = users[6].Id },
                new ConversationParticipant { ConversationId = conversations[3].Id, UserId = users[7].Id }
            };
            
            db.ConversationParticipants.AddRange(participants);
            await db.SaveChangesAsync();

            // Seed messages for realistic conversation history
            var messages = new List<Message>
            {
                // Conversation 1 messages (Alice and Bob about desk)
                new Message { ConversationId = conversations[0].Id, SenderId = users[0].Id, Content = "Hi Bob! I saw your study desk listing. Is it still available?", Timestamp = DateTime.UtcNow.AddHours(-2) },
                new Message { ConversationId = conversations[0].Id, SenderId = users[1].Id, Content = "Hi Alice! Yes, it's still available. Are you interested?", Timestamp = DateTime.UtcNow.AddHours(-2).AddMinutes(5) },
                new Message { ConversationId = conversations[0].Id, SenderId = users[0].Id, Content = "Definitely! Could I see it sometime today?", Timestamp = DateTime.UtcNow.AddHours(-2).AddMinutes(10) },
                new Message { ConversationId = conversations[0].Id, SenderId = users[1].Id, Content = "Of course! I'm in Anderson Hall room 205. When works for you?", Timestamp = DateTime.UtcNow.AddHours(-1).AddMinutes(-45) },
                new Message { ConversationId = conversations[0].Id, SenderId = users[0].Id, Content = "How about 3 PM today? I can meet you in the lobby.", Timestamp = DateTime.UtcNow.AddHours(-1).AddMinutes(-30) },
                new Message { ConversationId = conversations[0].Id, SenderId = users[1].Id, Content = "Sure, I'll bring it to the lobby tomorrow at 3 PM.", Timestamp = DateTime.UtcNow.AddMinutes(-30) },
                
                // Conversation 2 messages (Carol and David about bike)
                new Message { ConversationId = conversations[1].Id, SenderId = users[2].Id, Content = "Hey David, I'm interested in your mountain bike.", Timestamp = DateTime.UtcNow.AddHours(-3) },
                new Message { ConversationId = conversations[1].Id, SenderId = users[3].Id, Content = "Great! It's in excellent condition. Would you like to see it?", Timestamp = DateTime.UtcNow.AddHours(-3).AddMinutes(15) },
                new Message { ConversationId = conversations[1].Id, SenderId = users[2].Id, Content = "Is the bike still available?", Timestamp = DateTime.UtcNow.AddMinutes(-120) },
                
                // Conversation 3 messages (Emma and Frank about textbooks)
                new Message { ConversationId = conversations[2].Id, SenderId = users[4].Id, Content = "Hi Frank! Are those free textbooks still available?", Timestamp = DateTime.UtcNow.AddHours(-1) },
                new Message { ConversationId = conversations[2].Id, SenderId = users[5].Id, Content = "Yes they are! Calculus and Physics. When can you pick them up?", Timestamp = DateTime.UtcNow.AddHours(-1).AddMinutes(10) },
                new Message { ConversationId = conversations[2].Id, SenderId = users[4].Id, Content = "Perfect! I need both. Can I get them after my class around 4 PM?", Timestamp = DateTime.UtcNow.AddMinutes(-50) },
                new Message { ConversationId = conversations[2].Id, SenderId = users[5].Id, Content = "Absolutely! I'll be in my room. Johnson Hall 314.", Timestamp = DateTime.UtcNow.AddMinutes(-47) },
                new Message { ConversationId = conversations[2].Id, SenderId = users[4].Id, Content = "Thanks for the quick response!", Timestamp = DateTime.UtcNow.AddMinutes(-45) },
                
                // Conversation 4 messages (Grace and Henry about gaming monitor)
                new Message { ConversationId = conversations[3].Id, SenderId = users[6].Id, Content = "Hey Henry, interested in your gaming monitor. Is $200 firm?", Timestamp = DateTime.UtcNow.AddMinutes(-30) },
                new Message { ConversationId = conversations[3].Id, SenderId = users[7].Id, Content = "Hi Grace! I'm open to reasonable offers. What did you have in mind?", Timestamp = DateTime.UtcNow.AddMinutes(-25) },
                new Message { ConversationId = conversations[3].Id, SenderId = users[6].Id, Content = "What's your best price?", Timestamp = DateTime.UtcNow.AddMinutes(-15) }
            };
            
            db.Messages.AddRange(messages);
            await db.SaveChangesAsync();

            Console.WriteLine($"[DevSeeder] Successfully seeded:");
            Console.WriteLine($"  - {users.Count} users (including 1 admin, 1 banned)");
            Console.WriteLine($"  - {items.Count} items across {categories.Length} categories");
            Console.WriteLine($"  - {itemImages.Count} item images");
            Console.WriteLine($"  - {conversations.Count} conversations");
            Console.WriteLine($"  - {messages.Count} messages");
            Console.WriteLine($"[DevSeeder] Test login: any user with email/password = 'dummyhash'");
        }

        private static string HashPassword(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }
}
