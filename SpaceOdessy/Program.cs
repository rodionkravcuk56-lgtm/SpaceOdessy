using SDL3;
using System.Diagnostics;
using System.Drawing;

const float kPlayerHeight = 10;
const int kScreenHeight = 600;

SDL.Init(SDL.InitFlags.Video);
TTF.Init();

var game = new Game()
{
    Player = new Player
    {
        Size = new SizeF
        {
            Width = 100,
            Height = kPlayerHeight,
        },
        Position = new PointF
        {
            X = 50,
            Y = kScreenHeight - kPlayerHeight - 10,
        },
        Speed = 300f,
        Color = Color.Purple,
    },
    ScreenSize = new Size
    {
        Width = 800,
        Height = kScreenHeight,
    },
};

SDL.CreateWindowAndRenderer(
    "SpaceOdessy",
    game.ScreenSize.Width, game.ScreenSize.Height,
    0, out var window, out var renderer);

var coinTexture = Image.LoadTexture(renderer, "assets/coin.png");
SDL.GetTextureSize(coinTexture, out var coinW, out var coinH);
var coinSize = new SizeF
{
    Width = coinW,
    Height = coinH
};

var font = TTF.OpenFont("assets/OpenSans-Regular.ttf", 32);
var textEngine = TTF.CreateRendererTextEngine(renderer);

var objectSpeed = 100f;

var random = new Random();

var lastTime = SDL.GetTicksNS();

var fpsTimer = Stopwatch.StartNew();
var renderCount = 0;

while (game.IsRunning)
{
    var currentTime = SDL.GetTicksNS();
    var dt = (float)(currentTime - lastTime) / 1_000_000_000;
    lastTime = currentTime;

    while (SDL.PollEvent(out var @event))
    {
        switch (@event.Type)
        {
            case (uint)SDL.EventType.Quit:
                game.IsRunning = false;
                break;
            default:
                // Ignore
                break;
        }
    }

    var timeScale = 1.0f;
    var keyboardState = SDL.GetKeyboardState(out var _);
    if (keyboardState[(int)SDL.Scancode.Space])
    {
        timeScale = 2.0f;
    }

    game.Update(
        dt * timeScale, random,
        objectSpeed, coinSize);
    game.Render(
        renderer,
        coinTexture, coinSize,
        textEngine, font);

    renderCount++;
    if (fpsTimer.Elapsed > TimeSpan.FromSeconds(1))
    {
        Console.WriteLine($"FPS: {renderCount}");
        fpsTimer.Restart();
        renderCount = 0;
    }
}

TTF.CloseFont(font);
SDL.DestroyTexture(coinTexture);

TTF.DestroyRendererTextEngine(textEngine);

SDL.DestroyWindow(window);
SDL.DestroyRenderer(renderer);

TTF.Quit();
SDL.Quit();

class GameObject
{
    public PointF Position { get; set; }

    public SizeF Size { get; set; }

    public Color Color { get; set; }

    public bool IsAsteroid { get; set; }
}

class Player
{
    public PointF Position { get; set; }

    public SizeF Size { get; set; }

    public float Speed { get; set; }

    public Color Color { get; set; }
}


class Game
{
    const byte kSDLAlphaOpaque = (byte)SDL.AlphaOpaque;

    public bool IsRunning { get; set; } = true;
    
    public Player? Player { get; set; }

    public Size ScreenSize { get; set; }

    public List<GameObject> gameObjects = [];

    int score = 0;

    Color background = Color.DarkOrange;

    TimeSpan spawnRate = TimeSpan.FromSeconds(3);
    Stopwatch spawnTimer = Stopwatch.StartNew();

    public void Update(
        float dt,
        Random random, float objectSpeed,
        SizeF coinSize)
    {
        if (Player is null)
        {
            IsRunning = false;
            return;
        }

        var keyboardState = SDL.GetKeyboardState(out var _);
        var positionDelta = 0;
        if (keyboardState[(int)SDL.Scancode.Left])
        {
            positionDelta += -1;
        }
        if (keyboardState[(int)SDL.Scancode.Right])
        {
            positionDelta += 1;
        }

        var newPosition = Player.Position;
        newPosition.X += positionDelta * Player.Speed * dt;
        Player.Position = newPosition;

        if (spawnTimer.Elapsed > spawnRate)
        {
            const float asteroidSize = 50;

            var isAsteroid = random.Next(2) == 0;
            if (isAsteroid)
            {
                gameObjects.Add(new GameObject
                {
                    Size = new SizeF(asteroidSize, asteroidSize),
                    Color = Color.DarkGreen,
                    Position = new PointF((float)random.NextDouble() * (ScreenSize.Width - asteroidSize), 0),
                    IsAsteroid = true,
                });
            }
            else
            {
                gameObjects.Add(new GameObject
                {
                    Size = new SizeF(coinSize.Width, coinSize.Height),
                    Color = Color.LightPink,
                    Position = new PointF((float)random.NextDouble() * (ScreenSize.Width - coinSize.Width), 0),
                    IsAsteroid = false,
                });
            }

            spawnTimer.Restart();
        }

        for (int index = gameObjects.Count - 1; index >= 0; index--)
        {
            var gameObject = gameObjects[index];
            gameObject.Position = new PointF(
                gameObject.Position.X, gameObject.Position.Y + dt * objectSpeed);
            if (gameObject.Position.Y > ScreenSize.Height)
            {
                gameObjects.RemoveAt(index);
                continue;
            }

            if (RectangleF.Intersect(
                new RectangleF(Player.Position, Player.Size),
                new RectangleF(gameObject.Position, gameObject.Size)) != RectangleF.Empty)
            {
                if (gameObject.IsAsteroid)
                {
                    IsRunning = false;
                    Console.WriteLine($"GAME OVER! Score - {score}");
                    break;
                }
                else
                {
                    score += 10;
                    gameObjects.RemoveAt(index);
                    continue;
                }
            }
        }
    }

    public void Render(
        nint renderer,
        nint coinTexture, SizeF coinSize,
        nint textEngine, nint font)
    {
        SDL.SetRenderDrawColor(renderer, background.R, background.G, background.B, kSDLAlphaOpaque);
        SDL.RenderClear(renderer);
        foreach (var gameObject in gameObjects)
        {
            var objectColor = gameObject.Color;
            if (gameObject.IsAsteroid)
            {
                SDL.SetRenderDrawColor(
                    renderer, objectColor.R, objectColor.G, objectColor.B, kSDLAlphaOpaque);
                SDL.RenderFillRect(renderer, new SDL.FRect
                {
                    X = gameObject.Position.X,
                    Y = gameObject.Position.Y,
                    W = gameObject.Size.Width,
                    H = gameObject.Size.Height,
                });
            }
            else
            {
                SDL.SetTextureColorMod(coinTexture, objectColor.R, objectColor.G, objectColor.B);
                SDL.RenderTexture(
                    renderer, coinTexture,
                    new SDL.FRect
                    {
                        X = 0,
                        Y = 0,
                        W = coinSize.Width,
                        H = coinSize.Height,
                    },
                    new SDL.FRect
                    {
                        X = gameObject.Position.X,
                        Y = gameObject.Position.Y,
                        W = gameObject.Size.Width,
                        H = gameObject.Size.Height,
                    }
                );
            }
        }

        if (Player is not null)
        {
            SDL.SetRenderDrawColor(renderer, Player.Color.R, Player.Color.G, Player.Color.B, kSDLAlphaOpaque);
            SDL.RenderFillRect(renderer, new SDL.FRect
            {
                X = Player.Position.X,
                Y = Player.Position.Y,
                W = Player.Size.Width,
                H = Player.Size.Height,
            });
        }

        var text = $"Score: {score}";
        var sdlText = TTF.CreateText(textEngine, font, text, (nuint)text.Length);
        TTF.DrawRendererText(sdlText, 0, 0);
        TTF.DestroyText(sdlText);

        SDL.RenderPresent(renderer);
    }
}