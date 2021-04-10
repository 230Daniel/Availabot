using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Qmmands;

namespace DisqordTestBot.Commands
{
    public class TestCommands : DiscordGuildModuleBase
    {
        ILogger _logger;

        public TestCommands(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger("TestCommands");
        }

        [Command("Recreate")]
        public async Task<DiscordCommandResult> Recreate()
        {
            if(Context.GuildId != 689433540819288084) return null;

            await Context.Channel.DeleteAsync();

            await Context.Guild.CreateTextChannelAsync(Context.Channel.Name, x =>
            {
                x.IsNsfw = Context.Channel.IsNsfw;
                x.ParentId = Optional.FromNullable(Context.Channel.CategoryId);
                x.Position = Context.Channel.Position;
                x.Slowmode = Context.Channel.Slowmode;
                x.Topic = Context.Channel.Topic;
                x.Overwrites = Context.Channel.Overwrites.Select(y => new LocalOverwrite(y.TargetId, y.TargetType, y.Permissions)).ToList().AsReadOnly();
            });

            return null;
        }

        [Command("cool"), RequireAuthorGuildPermissions(Permission.Administrator), Cooldown(2, 10, CooldownMeasure.Seconds, CooldownBucketType.Channel)]
        public async Task Cool(int number, [Remainder] string phrase = "cool")
        {
            string res = "";
            for (int i = 0; i < number; i++)
            {
                res += phrase + " ";
            }

            _logger.LogDebug(res);
            await Response(res);
        }

        [Command("SpamNicely")]
        public async Task SpamNicely(int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Context.Channel.ModifyAsync(x => x.Topic = i.ToString());
            }
        }

        [Command("UseWeirdConstructor")]
        public async Task UseWeirdConstructor()
        {
            LocalMessageBuilder builder = new LocalMessageBuilder()
                .WithContent("")
                .WithEmbed(
                    new LocalEmbedBuilder()
                        .WithDescription("Cool Embed")
                );

            await Context.Channel.SendMessageAsync(builder.Build());
        }

        [Command("naughtsandcrosses", "tictactoe")]
        public async Task NaughtsAndCrosses([RequireNotBot] IMember playerTwo)
        {
            IMember playerOne = Context.Author;

            Board board = new Board(playerOne, playerTwo);

            LocalMessageBuilder messageBuilder = new LocalMessageBuilder()
                .WithContent($"Please wait...\n{board.RepresentBoard()}");

            IEmoji[] emojis = {
                new LocalEmoji("1️⃣"),
                new LocalEmoji("2️⃣"),
                new LocalEmoji("3️⃣"),
                new LocalEmoji("4️⃣"),
                new LocalEmoji("5️⃣"),
                new LocalEmoji("6️⃣"),
                new LocalEmoji("7️⃣"),
                new LocalEmoji("8️⃣"),
                new LocalEmoji("9️⃣")
            };

            IUserMessage message = await Context.Channel.SendMessageAsync(messageBuilder.Build());
            foreach (IEmoji emoji in emojis)
            {
                await message.AddReactionAsync(emoji);
            }

            await message.ModifyAsync(x => x.Content = $"{board.NextPlayer().Mention}'s turn\n{board.RepresentBoard()}");

            while (!board.IsFinished())
            {
                ReactionAddedEventArgs nextEmoji = await message.WaitForReactionAsync(x => x.UserId == board.NextPlayer().Id && emojis.Contains(x.Emoji), TimeSpan.FromMinutes(1));

                if (nextEmoji is null)
                {
                    await message.ModifyAsync(x => x.Content = $"{board.NextPlayer().Mention} didn't respond in time :(\n{board.RepresentBoard()}");
                    await message.ClearReactionsAsync();
                    return;
                }
                if (board.TryTakeTurn(Array.IndexOf(emojis, nextEmoji.Emoji) + 1))
                {
                    await message.ModifyAsync(x => x.Content = $"{board.NextPlayer().Mention}'s turn\n{board.RepresentBoard()}");
                }
                else
                {
                    await message.ModifyAsync(x => x.Content = $"{board.NextPlayer().Mention}'s turn\n{board.RepresentBoard()}Invalid input, please try again");
                }
            }

            await message.ModifyAsync(x => x.Content = $"{board.GetResult()}\n{board.RepresentBoard()}");
            await message.ClearReactionsAsync();
        }
    }

    class Board
    {
        Square[] _squares;
        int _turnNumber;
        IMember _playerOne;
        IMember _playerTwo;

        public Board(IMember playerOne, IMember playerTwo)
        {
            _squares = new Square[9];
            _playerOne = playerOne;
            _playerTwo = playerTwo;
        }

        public string RepresentBoard()
        {
            string representation = "";

            for(int i = 0; i < _squares.Length; i++)
            {
                representation += RepresentSquare(_squares[i], i + 1);
                if ((i + 1) % 3 == 0) representation += "\n";
            }

            return representation;
        }

        public bool IsFinished()
        {
            return IsWon() || IsDrawn();
        }

        bool IsWon()
        {
            return AreEqual(0, 1, 2) || AreEqual(3, 4, 5) || AreEqual(6, 7, 8) ||
                   AreEqual(0, 3, 6) || AreEqual(1, 4, 7) || AreEqual(2, 5, 8) ||
                   AreEqual(0, 4, 8) || AreEqual(2, 4, 6);
        }

        bool IsDrawn()
        {
            return _squares.All(x => x != Square.Empty);
        }

        public string GetResult()
        {
            return IsDrawn() ? "The game was a draw. Lame." : $"The winner is {NextPlayer().Mention}!";
        }

        bool AreEqual(int one, int two, int three)
        {
            return _squares[one] != Square.Empty && _squares[one] == _squares[two] && _squares[two] == _squares[three];
        }

        public IMember NextPlayer()
        {
            return _turnNumber % 2 == 0 ? _playerOne : _playerTwo;
        }

        public bool TryTakeTurn(int square)
        {
            if (square <= 9 && square >= 1 && _squares[square-1] == Square.Empty)
            {
                _squares[square - 1] = _turnNumber % 2 == 0 ? Square.Cross : Square.Naught;
                if(!IsFinished()) _turnNumber++;
                return true;
            }
            else return false;
        }

        string RepresentSquare(Square square, int number)
        {
            return square switch
            {
                Square.Empty => RepresentNumber(number),
                Square.Cross => "🇽 ",
                Square.Naught => "🇴 ",
                _ => throw new ArgumentOutOfRangeException(nameof(square), square, null)
            };
        }

        string RepresentNumber(int number)
        {
            return number switch
            {
                1 => "1️⃣ ",
                2 => "2️⃣ ",
                3 => "3️⃣ ",
                4 => "4️⃣ ",
                5 => "5️⃣ ",
                6 => "6️⃣ ",
                7 => "7️⃣ ",
                8 => "8️⃣ ",
                9 => "9️⃣ ",
                _ => throw new ArgumentOutOfRangeException(nameof(number), number, null)
            };
        }
    }

    enum Square
    {
        Empty,
        Cross,
        Naught
    }
}
