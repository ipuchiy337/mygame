using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// Процедурная генерация карты в духе Megabonk: почти ровная земля с мягкими холмами,
/// прямоугольные плато с отвесными стенами (иногда в два яруса), пандусы-плиты на плато,
/// границы, точка спавна игрока и объекты по категориям.
/// Все параметры настраиваются в инспекторе. Кнопки "Сгенерировать"/"Очистить" — в MapGeneratorEditor.
[ExecuteAlways] // в редакторе превью карты строится само при открытии сцены (если карта не хранится в сцене)
public class MapGenerator : MonoBehaviour
{
    /// Как выглядит объект без префаба: один примитив или составная фигура из примитивов.
    public enum Look { Primitive, Tree, Boulder, Ruin, Bush, Log, Pillar, Tuft, Mushroom, Crate, Barrel, Lantern, Campfire, Arch, Tent, Cart, Well }

    [Serializable]
    public class SpawnCategory
    {
        public string name = "Категория";
        public bool enabled = true;

        [Tooltip("Префаб объекта. Если пусто — ставится фигура из примитивов (см. look).")]
        public GameObject prefab;
        [Tooltip("Primitive — один примитив; Tree — ствол и крона из кубов; Boulder — наклонённая глыба; Ruin — обломки стены; Bush — куст; Log — поваленное бревно; Pillar — каменный столб; Tuft — пучок травы (иногда с цветком); Mushroom — грибы; Crate — ящики; Barrel — бочка; Lantern — фонарь на столбе; Campfire — костёр; Arch — каменная арка; Tent — палатка; Cart — телега; Well — колодец")]
        public Look look = Look.Primitive;
        public PrimitiveType primitive = PrimitiveType.Cube;
        public Color color = Color.white;
        [Tooltip("Свечение (0 — нет). Светящиеся объекты заметнее, bloom подхватывает их")]
        [Min(0f)] public float glow = 0f;
        [Tooltip("Размер (localScale) объекта")]
        public Vector3 size = Vector3.one;
        [Tooltip("Случайный множитель размера: от X до Y")]
        public Vector2 scaleRange = Vector2.one;

        [Header("Количество")]
        [Min(0)] public int minCount = 1;
        [Min(0)] public int maxCount = 3;
        [Tooltip("Шанс появления каждого объекта сверх минимума. Минимум ставится всегда (если хватает места).")]
        [Range(0f, 1f)] public float spawnChance = 1f;

        [Header("Размещение")]
        [Tooltip("Свободный радиус вокруг объекта — другие объекты не встанут ближе")]
        [Min(0f)] public float clearRadius = 2f;
        [Tooltip("Максимальный наклон земли в градусах")]
        [Range(0f, 90f)] public float maxSlope = 30f;
        [Tooltip("Ставить только туда, куда игрок может дойти пешком от точки спавна (и вернуться)")]
        public bool mustBeReachable = true;
        public bool randomYRotation = true;
    }

    [Header("Сид")]
    public bool randomSeed = true;
    public int seed = 12345;
    public bool generateOnStart = true;

    [Header("Размер карты")]
    [Tooltip("Ширина (X) и длина (Z) карты в метрах")]
    public Vector2 mapSize = new Vector2(200f, 200f);
    [Tooltip("Количество клеток меша земли по каждой стороне")]
    [Range(16, 512)] public int resolution = 160;
    [Tooltip("Отступ от края, в котором ничего не спавнится")]
    [Min(0f)] public float edgeMargin = 8f;

    [Header("Земля (холмы)")]
    [Tooltip("Высота холмов. В Megabonk земля почти ровная, высоту дают плато")]
    [Min(0f)] public float heightScale = 5f;
    [Tooltip("Меньше — шире холмы")]
    [Min(0.0001f)] public float noiseScale = 0.012f;
    [Range(1, 8)] public int octaves = 3;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Min(1f)] public float lacunarity = 2f;
    [Tooltip("Степень > 1 делает низины плоскими, а холмы — резче")]
    [Range(0.5f, 4f)] public float heightPower = 1f;
    public Color groundColor = new Color(0.3f, 0.46f, 0.24f);

    [Header("Разнообразие рельефа")]
    [Tooltip("Контраст между ровными полянами и холмистыми зонами. 0 — холмы везде одинаковые")]
    [Range(0f, 1f)] public float reliefVariation = 0.8f;
    [Tooltip("Размер зон: меньше — крупнее поляны и холмистые участки")]
    [Min(0.0001f)] public float reliefVariationScale = 0.007f;
    [Tooltip("Отдельные холмы-курганы: от X до Y штук")]
    public Vector2Int moundCount = new Vector2Int(3, 6);
    [Tooltip("Радиус кургана: от X до Y метров")]
    public Vector2 moundRadius = new Vector2(10f, 24f);
    [Tooltip("Высота кургана: от X до Y метров (ограничивается радиусом, чтобы склон оставался проходимым)")]
    public Vector2 moundHeight = new Vector2(3f, 7f);
    [Tooltip("Низины-котловины: от X до Y штук")]
    public Vector2Int basinCount = new Vector2Int(2, 4);
    [Tooltip("Радиус низины: от X до Y метров")]
    public Vector2 basinRadius = new Vector2(12f, 26f);
    [Tooltip("Глубина низины: от X до Y метров")]
    public Vector2 basinDepth = new Vector2(2f, 4f);

    [Header("Детали рельефа")]
    [Tooltip("Шанс, что в низине будет пруд")]
    [Range(0f, 1f)] public float pondChance = 0.7f;
    public Color waterColor = new Color(0.24f, 0.48f, 0.62f);
    [Tooltip("Каменная осыпь у подножия каждого плато: от X до Y камней")]
    public Vector2Int rocksAtPlateauBase = new Vector2Int(2, 5);
    public Color baseRockColor = new Color(0.52f, 0.5f, 0.48f);
    [Tooltip("Тропинки от спавна к магазинам и от магазинов к столам апгрейда")]
    public bool paths = true;
    [Min(0.5f)] public float pathWidth = 2.4f;
    public Color pathColor = new Color(0.5f, 0.4f, 0.27f);
    [Tooltip("Лианы на стенах плато: примерно штук на 10 м стены")]
    [Min(0f)] public float vinesPer10m = 1.5f;
    public Color vineColor = new Color(0.2f, 0.4f, 0.17f);
    [Tooltip("Ящики и бочки вокруг магазинов и столов апгрейда: от X до Y")]
    public Vector2Int propsAroundShops = new Vector2Int(2, 5);
    [Tooltip("Фонари у магазинов и столов апгрейда (и иногда у руин)")]
    public bool lanterns = true;
    public Color lanternLightColor = new Color(1f, 0.58f, 0.18f);

    [Header("Детали: окружение")]
    [Tooltip("Костры (чаще у руин): от X до Y")]
    public Vector2Int campfireCount = new Vector2Int(1, 3);
    [Tooltip("Доля тропинок, вдоль которых стоит заборчик")]
    [Range(0f, 1f)] public float fenceOnPathChance = 0.5f;
    [Tooltip("Указатель у точки спавна, таблички смотрят на магазины")]
    public bool spawnSignpost = true;
    [Tooltip("Шанс скального выхода на вершине кургана")]
    [Range(0f, 1f)] public float outcropChance = 0.6f;
    [Tooltip("Цветочные поляны: от X до Y")]
    public Vector2Int flowerMeadows = new Vector2Int(3, 6);
    [Tooltip("Кусты и камни вдоль стен-границ карты")]
    public bool borderVegetation = true;
    [Tooltip("Каменные арки: от X до Y")]
    public Vector2Int archCount = new Vector2Int(1, 2);
    public Color woodColor = new Color(0.55f, 0.4f, 0.25f);

    [Header("Детали: мосты, пруды, кости, рельсы")]
    [Tooltip("Деревянные мосты между близкими плато примерно одной высоты: от X до Y")]
    public Vector2Int bridgeCount = new Vector2Int(1, 3);
    [Tooltip("Камыши и кувшинки у прудов")]
    public bool pondPlants = true;
    [Tooltip("Кучки костей (чаще у руин): от X до Y")]
    public Vector2Int bonePiles = new Vector2Int(2, 5);
    [Tooltip("Приподнятые рельсы на опорах (декор, как в Megabonk): от X до Y")]
    public Vector2Int railCount = new Vector2Int(1, 2);
    public Color railColor = new Color(0.62f, 0.64f, 0.7f);

    [Header("Детали: лагеря и ориентиры")]
    [Tooltip("Дым и искры над кострами, светлячки над прудами и цветочными полянами")]
    public bool ambientParticles = true;
    [Tooltip("Палатка рядом с каждым костром")]
    public bool tentsAtCampfires = true;
    [Tooltip("Телеги у руин и магазинов: от X до Y")]
    public Vector2Int cartCount = new Vector2Int(1, 3);
    [Tooltip("Колодцы: от X до Y")]
    public Vector2Int wellCount = new Vector2Int(0, 1);
    [Tooltip("Флаги на самых высоких плато (ориентиры)")]
    [Min(0)] public int flagsOnHighest = 3;
    [Tooltip("Маленькие кладбища: от X до Y")]
    public Vector2Int graveyardCount = new Vector2Int(0, 1);

    [Tooltip("Имена категорий, к которым ведут тропинки и вокруг которых ставятся ящики/фонари")]
    public string shopCategory = "Магазин оружия";
    public string upgradeCategory = "Стол апгрейда";
    public string ruinCategory = "Руины";

    [Header("Горы (гребни)")]
    [Tooltip("Высота горных гребней. 0 — без гор")]
    [Min(0f)] public float mountainHeight = 0f;
    [Tooltip("Меньше — длиннее и шире гребни")]
    [Min(0.0001f)] public float mountainScale = 0.009f;
    [Tooltip("Чем выше, тем реже и уже гребни")]
    [Range(0f, 0.95f)] public float mountainThreshold = 0.55f;

    [Header("Подъём к краям")]
    [Tooltip("На сколько земля поднимается у границ карты. 0 — без подъёма")]
    [Min(0f)] public float edgeRiseHeight = 6f;
    [Tooltip("Ширина полосы подъёма от края")]
    [Min(0.01f)] public float edgeRiseWidth = 20f;

    [Header("Плато (блоки с отвесными стенами)")]
    [Min(0)] public int plateauMin = 6;
    [Min(0)] public int plateauMax = 10;
    [Tooltip("Длина стороны плато: от X до Y метров")]
    public Vector2 plateauSize = new Vector2(14f, 36f);
    [Tooltip("Высота плато над полом: от X до Y метров")]
    public Vector2 plateauHeight = new Vector2(4f, 9f);
    [Tooltip("Насколько сильно повёрнуты плато: 0 — все по сетке, 45 — любой угол")]
    [Range(0f, 45f)] public float plateauRotation = 45f;
    [Tooltip("Шанс пристройки: плато становится Г- или Т-образным")]
    [Range(0f, 1f)] public float plateauExtensionChance = 0.5f;
    [Tooltip("Шанс, что на плато появится второй ярус")]
    [Range(0f, 1f)] public float secondTierChance = 0.4f;
    [Tooltip("Минимальный проход между плато")]
    [Min(0f)] public float plateauGap = 6f;
    public Color plateauSideColor = new Color(0.56f, 0.47f, 0.38f);
    public Color plateauTopColor = new Color(0.34f, 0.5f, 0.26f);

    [Header("Подъёмы на плато")]
    [Tooltip("Сколько подъёмов максимум у одного плато (минимум всегда 1)")]
    [Min(1)] public int rampsPerPlateauMax = 3;
    [Tooltip("Ширина подъёма: от X до Y метров")]
    public Vector2 rampWidth = new Vector2(3.5f, 7f);
    [Tooltip("Наклон подъёма: от X до Y градусов. Не больше walkableSlope, иначе по нему не зайти")]
    public Vector2 rampAngle = new Vector2(16f, 32f);
    [Tooltip("Доля лестниц среди подъёмов")]
    [Range(0f, 1f)] public float stairsChance = 0.3f;
    [Tooltip("Доля пандусов вдоль стены (с площадкой наверху). Остальные — плиты в лоб к стене")]
    [Range(0f, 1f)] public float alongWallChance = 0.3f;
    [Tooltip("Высота ступени. Должна быть меньше stepOffset контроллера игрока")]
    [Range(0.1f, 0.5f)] public float stairStepHeight = 0.3f;
    public Color rampColor = new Color(0.55f, 0.5f, 0.45f);

    [Header("Внешний вид")]
    [Tooltip("Пиксельные текстуры из кода (трава, земля, камень, кирпич, листва). Выкл — плоские цвета")]
    public bool pixelTextures = true;
    [Tooltip("Сколько метров занимает один повтор текстуры")]
    [Min(0.25f)] public float textureTileMeters = 2f;
    public Color trunkColor = new Color(0.42f, 0.28f, 0.18f);

    [Header("Зомби: точки спавна")]
    [Tooltip("Точки спавна зомби: от X до Y")]
    public Vector2Int zombieSpawnCount = new Vector2Int(14, 22);
    [Tooltip("Не ближе этого расстояния к точке спавна игрока, м")]
    [Min(0f)] public float zombieMinDistanceFromPlayer = 40f;
    [Tooltip("Минимальное расстояние между точками спавна зомби, м")]
    [Min(1f)] public float zombieSpawnSpacing = 12f;
    [Tooltip("Доля точек-гнёзд у кладбищ, костей и руин (остальные — ближе к краям карты)")]
    [Range(0f, 1f)] public float zombieNestShare = 0.4f;
    [Tooltip("Показывать точки спавна разрытой землёй (без коллайдера)")]
    public bool zombieSpawnMarkers = true;

    [Header("Оптимизация")]
    [Tooltip("Склеивать статичный декор и постройки в общие меши по материалу и участкам карты — в разы меньше отрисовок. Коллайдеры остаются на своих объектах. Игровые объекты (магазины, лут, хилки, столы) не склеиваются")]
    public bool combineMeshes = true;
    [Tooltip("Размер участка карты для склейки: меньше — точнее отсечение по камере, но больше отрисовок")]
    [Min(10f)] public float combineCellSize = 50f;
    [Tooltip("Мелкие объекты (трава, цветы, лианы) не отбрасывают тень")]
    public bool noShadowsOnSmall = true;
    [Tooltip("Сохранять сгенерированную карту в файл сцены. Выкл — сцена лёгкая, а превью строится при открытии сцены с тем же сидом")]
    public bool keepGeneratedInScene = false;

    [Header("Проходимость")]
    [Tooltip("Максимальный уклон, по которому игрок заходит пешком. Должен совпадать со slopeLimit контроллера игрока")]
    [Range(10f, 80f)] public float walkableSlope = 45f;

    [Header("Границы")]
    [Min(0f)] public float wallHeight = 15f;
    public Color wallColor = new Color(0.4f, 0.4f, 0.45f);

    [Header("Игрок")]
    [Tooltip("Если задан — переносится в точку спавна. Если пусто — ставится капсула-заглушка.")]
    public Transform player;
    [Tooltip("Свободный радиус вокруг точки спавна игрока")]
    [Min(0f)] public float playerClearRadius = 15f;

    [Header("Объекты (порядок = приоритет размещения)")]
    public List<SpawnCategory> categories = new List<SpawnCategory>
    {
        new SpawnCategory { name = "Магазин оружия", primitive = PrimitiveType.Cube, color = new Color(0.2f, 0.4f, 1f), glow = 0.4f, size = new Vector3(6, 4, 6), minCount = 2, maxCount = 4, spawnChance = 0.7f, clearRadius = 10f, maxSlope = 15f },
        new SpawnCategory { name = "Стол апгрейда", primitive = PrimitiveType.Cube, color = new Color(0.7f, 0.2f, 1f), glow = 1.5f, size = new Vector3(3, 1, 2), minCount = 2, maxCount = 5, spawnChance = 0.6f, clearRadius = 6f, maxSlope = 15f },
        new SpawnCategory { name = "Хилка", primitive = PrimitiveType.Sphere, color = new Color(1f, 0.25f, 0.3f), glow = 2.5f, size = Vector3.one, minCount = 5, maxCount = 12, spawnChance = 0.7f, clearRadius = 3f, maxSlope = 35f },
        new SpawnCategory { name = "Лут", primitive = PrimitiveType.Cube, color = new Color(1f, 0.8f, 0.15f), glow = 1.5f, size = new Vector3(1.2f, 0.8f, 0.8f), minCount = 15, maxCount = 30, spawnChance = 0.8f, clearRadius = 2f, maxSlope = 30f },
        new SpawnCategory { name = "Руины", look = Look.Ruin, color = new Color(0.62f, 0.6f, 0.55f), size = new Vector3(6f, 3.5f, 1f), scaleRange = new Vector2(0.7f, 1.3f), minCount = 6, maxCount = 14, spawnChance = 0.7f, clearRadius = 4f, maxSlope = 20f, mustBeReachable = false },
        new SpawnCategory { name = "Дерево", look = Look.Tree, color = new Color(0.22f, 0.5f, 0.2f), size = new Vector3(1.5f, 6f, 1.5f), scaleRange = new Vector2(0.7f, 1.4f), minCount = 40, maxCount = 80, spawnChance = 0.9f, clearRadius = 3f, maxSlope = 40f, mustBeReachable = false },
        new SpawnCategory { name = "Пропс (камень)", look = Look.Boulder, color = new Color(0.55f, 0.55f, 0.58f), size = new Vector3(2f, 1.4f, 1.8f), scaleRange = new Vector2(0.5f, 1.6f), minCount = 20, maxCount = 50, spawnChance = 0.8f, clearRadius = 2f, maxSlope = 45f, mustBeReachable = false },
        new SpawnCategory { name = "Куст", look = Look.Bush, color = new Color(0.2f, 0.42f, 0.18f), size = new Vector3(1.6f, 1.1f, 1.6f), scaleRange = new Vector2(0.7f, 1.3f), minCount = 25, maxCount = 45, spawnChance = 0.8f, clearRadius = 1.5f, maxSlope = 35f, mustBeReachable = false },
        new SpawnCategory { name = "Бревно", look = Look.Log, color = new Color(0.45f, 0.32f, 0.2f), size = new Vector3(4f, 0.6f, 0.6f), scaleRange = new Vector2(0.7f, 1.3f), minCount = 6, maxCount = 12, spawnChance = 0.7f, clearRadius = 3f, maxSlope = 15f, mustBeReachable = false },
        new SpawnCategory { name = "Каменный столб", look = Look.Pillar, color = new Color(0.6f, 0.6f, 0.62f), size = new Vector3(2f, 7f, 2f), scaleRange = new Vector2(0.6f, 1.3f), minCount = 3, maxCount = 6, spawnChance = 0.6f, clearRadius = 4f, maxSlope = 25f, mustBeReachable = false },
        new SpawnCategory { name = "Грибы", look = Look.Mushroom, color = new Color(0.78f, 0.22f, 0.16f), size = new Vector3(0.6f, 0.7f, 0.6f), scaleRange = new Vector2(0.7f, 1.5f), minCount = 15, maxCount = 30, spawnChance = 0.8f, clearRadius = 1f, maxSlope = 35f, mustBeReachable = false },
        new SpawnCategory { name = "Трава", look = Look.Tuft, color = new Color(0.32f, 0.55f, 0.25f), size = new Vector3(1f, 0.5f, 1f), scaleRange = new Vector2(0.7f, 1.4f), minCount = 120, maxCount = 200, spawnChance = 0.9f, clearRadius = 0.6f, maxSlope = 45f, mustBeReachable = false },
    };

    /// Повёрнутый прямоугольник на плоскости XZ: центр, половины сторон, ось X (единичный вектор).
    struct Obb
    {
        public Vector2 c, h, ax;
        public Vector2 Az => new Vector2(-ax.y, ax.x);

        public bool Contains(Vector2 p, float margin = 0f)
        {
            var d = p - c;
            return Mathf.Abs(Vector2.Dot(d, ax)) <= h.x + margin && Mathf.Abs(Vector2.Dot(d, Az)) <= h.y + margin;
        }

        float Extent(Vector2 axis) => h.x * Mathf.Abs(Vector2.Dot(ax, axis)) + h.y * Mathf.Abs(Vector2.Dot(Az, axis));

        /// Пересечение с запасом margin (теорема о разделяющей оси).
        public bool Overlaps(Obb o, float margin)
        {
            var d = o.c - c;
            foreach (var axis in new[] { ax, Az, o.ax, o.Az })
                if (Mathf.Abs(Vector2.Dot(d, axis)) > Extent(axis) + o.Extent(axis) + margin) return false;
            return true;
        }

        public bool InsideOf(Obb outer)
        {
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                if (!outer.Contains(c + ax * (h.x * sx) + Az * (h.y * sz))) return false;
            return true;
        }

        public Vector2 Point(float u, float v) => c + ax * (h.x * u) + Az * (h.y * v); // u, v в [-1, 1]
        public Quaternion Rotation => Quaternion.LookRotation(new Vector3(Az.x, 0f, Az.y)); // локальная X -> ax
    }

    struct Block { public Obb box; public float top, bottom; }

    enum RampKind { Slab, Stairs, AlongWall }

    struct Ramp
    {
        public RampKind kind;
        public Vector2 foot, dir;   // dir — от подножия вверх по склону
        public Vector2 exit;        // куда игрок сходит с подъёма на плато
        public float length;        // длина склона по горизонтали
        public float landing;       // ровная площадка наверху (у пандуса вдоль стены)
        public float width, footH, topH, stepHeight;
        public Vector2 Top => foot + dir * length;
        public float Total => length + landing;
        public Obb Box => new Obb { c = foot + dir * (Total * 0.5f), h = new Vector2(Total * 0.5f, width * 0.5f), ax = dir };
        public int Steps => Mathf.Max(1, Mathf.CeilToInt((topH - footH) / stepHeight));

        /// Высота поверхности подъёма в точке или NaN, если точка не на нём (margin расширяет область).
        public float HeightAt(Vector2 p, float margin = 0f)
        {
            var local = p - foot;
            float along = Vector2.Dot(local, dir);
            float side = Mathf.Abs(local.x * dir.y - local.y * dir.x);
            if (along < -margin || along > Total + margin || side > width * 0.5f + margin) return float.NaN;
            if (along >= length) return topH;
            float t = Mathf.Clamp01(along / length);
            if (kind != RampKind.Stairs) return Mathf.Lerp(footH, topH, t);
            int n = Steps;
            return footH + (Mathf.Min(n - 1, Mathf.FloorToInt(t * n)) + 1) * (topH - footH) / n;
        }
    }

    const string RootName = "Generated";
    const int PlaceAttempts = 30;

    System.Random rng;
    float[,] meshHeights;
    float[,] surface;   // высота с учётом плато и пандусов в вершинах сетки
    bool[,] walkRegion; // самая большая связная область, где можно ходить туда и обратно
    Vector2 noiseOffset;
    Material baseMaterial;
    readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    readonly List<Vector3> occupied = new List<Vector3>(); // x, z = позиция, y = радиус
    readonly List<Block> blocks = new List<Block>();
    readonly List<Ramp> ramps = new List<Ramp>();

    /// Где игрок появился в последней генерации.
    public Vector3 PlayerSpawnPoint { get; private set; }
    /// Вызывается после каждой генерации (в т.ч. превью в редакторе) — сюда подписывается связка с игрой.
    public event Action Generated;
    /// Доля карты, по которой игрок свободно ходит (самая большая проходимая область).
    public float WalkableFraction { get; private set; }
    public int PlateauCount => blocks.Count;
    public int RampCount => ramps.Count;

    void Start()
    {
        if (!Application.isPlaying) return;
        if (generateOnStart) Generate();
        // Карта не хранится в сцене: без генерации при старте игра осталась бы без карты — строим с сохранённым сидом.
        else if (transform.Find(RootName) == null) GeneratePreview();
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        // Карта не хранится в сцене — после открытия сцены (и выхода из Play) строим превью с сохранённым сидом.
        // delayCall: создавать объекты прямо во время загрузки сцены Unity не любит.
        if (!Application.isPlaying && !keepGeneratedInScene)
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled && !Application.isPlaying && transform.Find(RootName) == null) GeneratePreview();
            };
#endif
    }

    /// Перестроить карту с текущим сидом (без выбора нового случайного).
    public void GeneratePreview()
    {
        bool wasRandom = randomSeed;
        randomSeed = false;
        Generate();
        randomSeed = wasRandom;
    }

    void OnValidate()
    {
        foreach (var c in categories)
        {
            c.maxCount = Mathf.Max(c.maxCount, c.minCount);
            c.scaleRange.y = Mathf.Max(c.scaleRange.y, c.scaleRange.x);
        }
        mapSize = Vector2.Max(mapSize, new Vector2(10f, 10f));
        plateauMax = Mathf.Max(plateauMax, plateauMin);
        plateauSize = new Vector2(Mathf.Max(2f, plateauSize.x), Mathf.Max(plateauSize.x, plateauSize.y));
        plateauHeight = new Vector2(Mathf.Max(0.5f, plateauHeight.x), Mathf.Max(plateauHeight.x, plateauHeight.y));
        rampWidth = new Vector2(Mathf.Max(1f, rampWidth.x), Mathf.Max(rampWidth.x, rampWidth.y));
        rampAngle = new Vector2(Mathf.Clamp(rampAngle.x, 5f, 60f), Mathf.Clamp(rampAngle.y, rampAngle.x, 60f));
    }

    public void Generate()
    {
        Clear();
        if (randomSeed) seed = Environment.TickCount;
        rng = new System.Random(seed);
        noiseOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));
        GenerateReliefFeatures();
        occupied.Clear();
        materials.Clear();
        blocks.Clear();
        ramps.Clear();

        var root = new GameObject(RootName).transform;
        root.SetParent(transform, false);

        BuildTerrain(root);
        BuildStructures(root);
        BuildWalkRegion();
        BuildWalls(root);
        BuildTerrainDetails(root);
        SpawnPlayer(root);
        foreach (var c in categories)
            if (c.enabled) SpawnCategoryObjects(c, root);
        BuildFinishingDetails(root);
        BuildSurroundings(root);
        BuildExtras(root);
        BuildCampsAndLandmarks(root);
        PlaceZombieSpawns(root); // до склейки: маркеры точек спавна тоже склеиваются
        if (combineMeshes) CombineStatic(root);

        // Сгенерированное не попадает в файл сцены (в Play это ни на что не влияет).
        if (!keepGeneratedInScene && !Application.isPlaying)
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags |= HideFlags.DontSaveInEditor;

        Physics.SyncTransforms();
        Generated?.Invoke();
    }

    /// Меши и материалы, созданные генерацией: удаляются при перегенерации, чтобы не копились в памяти редактора.
    readonly List<UnityEngine.Object> generatedAssets = new List<UnityEngine.Object>();

    public void Clear()
    {
        var old = transform.Find(RootName);
        if (old != null) Kill(old.gameObject);
        foreach (var o in generatedAssets) Kill(o);
        generatedAssets.Clear();
        materials.Clear();
        zombieSpawns.Clear();
    }

    /// Склеивает статичные меши в общие по (материал, участок карты, тень). Коллайдеры остаются на исходных объектах;
    /// чисто декоративные детали без коллайдера удаляются. Игровые категории и игрок не трогаются.
    void CombineStatic(Transform root)
    {
        var exclude = new List<Transform>();
        foreach (var c in categories)
            if (c.mustBeReachable || c.prefab != null) { var g = root.Find(c.name); if (g != null) exclude.Add(g); }
        var placeholder = root.Find("PlayerSpawn");
        if (placeholder != null) exclude.Add(placeholder);
        bool Excluded(Transform t)
        {
            for (; t != null && t != root; t = t.parent)
                if (exclude.Contains(t)) return true;
            return false;
        }

        var buckets = new Dictionary<(Material mat, int cx, int cz, bool shadows), List<CombineInstance>>();
        var vertexCounts = new Dictionary<(Material, int, int, bool), int>();
        var stripped = new List<MeshRenderer>();
        var toRoot = root.worldToLocalMatrix;
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable || mr.sharedMaterials.Length != 1) continue;
            if (mr.name == "Terrain" || Excluded(mr.transform)) continue;
            bool shadows = mr.shadowCastingMode != ShadowCastingMode.Off && !(noShadowsOnSmall && mr.bounds.size.magnitude < 1f);
            var lc = root.InverseTransformPoint(mr.bounds.center);
            var key = (mr.sharedMaterial, Mathf.FloorToInt((lc.x - MapMin.x) / combineCellSize), Mathf.FloorToInt((lc.z - MapMin.y) / combineCellSize), shadows);
            if (!buckets.TryGetValue(key, out var list)) { buckets[key] = list = new List<CombineInstance>(); vertexCounts[key] = 0; }
            list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = toRoot * mr.localToWorldMatrix });
            vertexCounts[key] += mf.sharedMesh.vertexCount;
            stripped.Add(mr);
        }

        var group = new GameObject("Combined").transform;
        group.SetParent(root, false);
        foreach (var kv in buckets)
        {
            var mesh = new Mesh
            {
                name = $"Combined_{kv.Key.mat.name}_{kv.Key.cx}_{kv.Key.cz}",
                indexFormat = vertexCounts[kv.Key] > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };
            mesh.CombineMeshes(kv.Value.ToArray(), true, true);
            mesh.RecalculateBounds();
            generatedAssets.Add(mesh);
            var go = new GameObject(mesh.name);
            go.transform.SetParent(group, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = kv.Key.mat;
            r.shadowCastingMode = kv.Key.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        // Исходники: рендер убираем, коллайдеры оставляем; декоративные детали без коллайдера — удаляем целиком.
        foreach (var mr in stripped)
        {
            var go = mr.gameObject;
            Kill(mr);
            Kill(go.GetComponent<MeshFilter>());
            int depth = 0;
            for (var t = go.transform; t != null && t != root; t = t.parent) depth++;
            if (depth >= 3 && go.GetComponent<Collider>() == null && go.transform.childCount == 0) Kill(go);
        }
    }

    // ---------- Земля ----------

    float NoiseHeight(float x, float z)
    {
        float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise((x + noiseOffset.x) * noiseScale * freq, (z + noiseOffset.y) * noiseScale * freq) * amp;
            norm += amp;
            amp *= persistence;
            freq *= lacunarity;
        }
        float h = Mathf.Pow(Mathf.Clamp01(sum / norm), heightPower) * heightScale;

        // Зоны: крупная маска делает одни участки ровными полянами, другие — холмистее.
        if (reliefVariation > 0f)
        {
            float zone = Mathf.PerlinNoise((x + noiseOffset.y * 0.5f) * reliefVariationScale, (z - noiseOffset.x * 0.5f) * reliefVariationScale);
            h *= Mathf.Lerp(1f - reliefVariation, 1f + reliefVariation, zone);
        }

        // Курганы (+) и низины (−): плавный купол smoothstep от центра к краю.
        foreach (var f in reliefFeatures)
        {
            float d = new Vector2(x - f.x, z - f.y).magnitude;
            if (d >= f.z) continue;
            float t = 1f - d / f.z;
            h += f.w * t * t * (3f - 2f * t);
        }

        // Гребни: ridged-шум (1 - |2n - 1|) даёт острые хребты, порог оставляет только их вершины.
        if (mountainHeight > 0f)
        {
            float n = Mathf.PerlinNoise((x - noiseOffset.y) * mountainScale, (z + noiseOffset.x) * mountainScale);
            float ridge = 1f - Mathf.Abs(2f * n - 1f);
            float t = Mathf.Clamp01((ridge - mountainThreshold) / (1f - mountainThreshold));
            h += t * t * mountainHeight;
        }

        // Подъём к краям: плавная "чаша" у границ.
        if (edgeRiseHeight > 0f)
        {
            float d = Mathf.Min(mapSize.x * 0.5f - Mathf.Abs(x), mapSize.y * 0.5f - Mathf.Abs(z));
            float t = 1f - Mathf.Clamp01(d / edgeRiseWidth);
            h += t * t * (3f - 2f * t) * edgeRiseHeight;
        }
        return h;
    }

    float MaxTerrainHeight => heightScale * (1f + reliefVariation) + moundHeight.y + mountainHeight + edgeRiseHeight;

    readonly List<Vector4> reliefFeatures = new List<Vector4>(); // (центр X, центр Z, радиус, высота: + курган, − низина)

    /// Курганы и низины. Свой генератор случайных чисел, чтобы не сдвигать расстановку плато и объектов для того же сида.
    void GenerateReliefFeatures()
    {
        reliefFeatures.Clear();
        var r = new System.Random(seed ^ 0x5EED);
        float R(Vector2 range) => range.x + (float)r.NextDouble() * (range.y - range.x);
        var half = mapSize * 0.5f - Vector2.one * edgeRiseWidth;
        void Add(int count, Vector2 radius, Vector2 amount, float sign)
        {
            for (int i = 0; i < count; i++)
            {
                float rad = R(radius);
                // Высота не больше 0.35 радиуса: максимальный уклон купола ~28°, по нему можно ходить и ставить объекты.
                float amp = Mathf.Min(R(amount), rad * 0.35f) * sign;
                reliefFeatures.Add(new Vector4(R(new Vector2(-half.x, half.x)), R(new Vector2(-half.y, half.y)), rad, amp));
            }
        }
        Add(r.Next(moundCount.x, Mathf.Max(moundCount.x, moundCount.y) + 1), moundRadius, moundHeight, 1f);
        Add(r.Next(basinCount.x, Mathf.Max(basinCount.x, basinCount.y) + 1), basinRadius, basinDepth, -1f);
    }

    Vector2 CellSize => new Vector2(mapSize.x / resolution, mapSize.y / resolution);
    Vector2 MapMin => -mapSize * 0.5f;

    void BuildTerrain(Transform root)
    {
        int n = resolution + 1;
        meshHeights = new float[n, n];
        var verts = new Vector3[n * n];
        var uvs = new Vector2[n * n];
        var cell = CellSize;

        for (int z = 0; z < n; z++)
        for (int x = 0; x < n; x++)
        {
            float px = MapMin.x + x * cell.x, pz = MapMin.y + z * cell.y;
            float h = meshHeights[x, z] = NoiseHeight(px, pz);
            verts[z * n + x] = new Vector3(px, h, pz);
            uvs[z * n + x] = new Vector2((float)x / resolution, (float)z / resolution);
        }

        var tris = new int[resolution * resolution * 6];
        int t = 0;
        for (int z = 0; z < resolution; z++)
        for (int x = 0; x < resolution; x++)
        {
            int a = z * n + x, b = a + n, c = b + 1, d = a + 1; // a(x,z) b(x,z+1) c(x+1,z+1) d(x+1,z)
            tris[t++] = a; tris[t++] = b; tris[t++] = c;
            tris[t++] = a; tris[t++] = c; tris[t++] = d;
        }

        var mesh = new Mesh { name = "MapTerrain", indexFormat = verts.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        generatedAssets.Add(mesh);
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject("Terrain");
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = GetMaterial(groundColor, Pattern.Grass, mapSize / textureTileMeters);
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    /// Высота меша земли (без плато) в локальных координатах — точная интерполяция по треугольнику.
    float MeshHeight(float x, float z)
    {
        var cell = CellSize;
        float gx = Mathf.Clamp((x - MapMin.x) / cell.x, 0f, resolution - 0.0001f);
        float gz = Mathf.Clamp((z - MapMin.y) / cell.y, 0f, resolution - 0.0001f);
        int ix = (int)gx, iz = (int)gz;
        float fx = gx - ix, fz = gz - iz;
        float h00 = meshHeights[ix, iz], h10 = meshHeights[ix + 1, iz], h01 = meshHeights[ix, iz + 1], h11 = meshHeights[ix + 1, iz + 1];
        return fz > fx
            ? h00 + (h11 - h01) * fx + (h01 - h00) * fz   // треугольник a-b-c
            : h00 + (h10 - h00) * fx + (h11 - h10) * fz;  // треугольник a-c-d
    }

    /// Высота поверхности, на которой стоит игрок: земля, крыша плато или пандус — что выше.
    public float GroundHeight(float x, float z)
    {
        float h = MeshHeight(x, z);
        var p = new Vector2(x, z);
        foreach (var b in blocks)
            if (b.top > h && b.box.Contains(p)) h = b.top;
        foreach (var r in ramps)
        {
            float rh = r.HeightAt(p);
            if (!float.IsNaN(rh) && rh > h) h = rh;
        }
        return h;
    }

    float SlopeAngle(float x, float z)
    {
        float e = Mathf.Max(CellSize.x, CellSize.y);
        float dx = (GroundHeight(x + e, z) - GroundHeight(x - e, z)) / (2f * e);
        float dz = (GroundHeight(x, z + e) - GroundHeight(x, z - e)) / (2f * e);
        return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
    }

    // ---------- Плато и подъёмы ----------

    float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);
    float Range(Vector2 r) => Range(r.x, r.y);
    Vector2 Dir(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

    /// Пересекается ли область с плато (кроме ignoreA/ignoreB) или подъёмами.
    bool Collides(Obb o, float blockMargin, float rampMargin, int ignoreA = -1, int ignoreB = -1, List<Ramp> pending = null)
    {
        for (int i = 0; i < blocks.Count; i++)
            if (i != ignoreA && i != ignoreB && o.Overlaps(blocks[i].box, blockMargin)) return true;
        foreach (var r in ramps) if (o.Overlaps(r.Box, rampMargin)) return true;
        if (pending != null) foreach (var r in pending) if (o.Overlaps(r.Box, rampMargin)) return true;
        return false;
    }

    /// Высота земли под областью: (самая высокая точка, самая низкая точка).
    Vector2 TerrainSpan(Obb box)
    {
        float hi = float.MinValue, lo = float.MaxValue;
        for (int i = 0; i <= 4; i++)
        for (int j = 0; j <= 4; j++)
        {
            var p = box.Point(i / 2f - 1f, j / 2f - 1f);
            float h = MeshHeight(p.x, p.y);
            hi = Mathf.Max(hi, h); lo = Mathf.Min(lo, h);
        }
        return new Vector2(hi, lo);
    }

    void BuildStructures(Transform root)
    {
        var area = new Obb { c = Vector2.zero, h = mapSize * 0.5f - Vector2.one * edgeMargin, ax = Vector2.right };
        int target = rng.Next(plateauMin, plateauMax + 1);
        int placedMain = 0;
        for (int i = 0; i < target; i++)
        for (int attempt = 0; attempt < PlaceAttempts; attempt++)
        {
            if (!TryPlateau(area, -1, plateauSize, out int mi)) continue;
            placedMain++;
            var main = blocks[mi];
            if (rng.NextDouble() < plateauExtensionChance) TryExtension(area, mi);
            if (rng.NextDouble() < secondTierChance)
            {
                // Второй ярус: меньший блок на крыше, его подъём стоит на крыше нижнего.
                var inner = main.box; inner.h -= Vector2.one * 1f;
                float maxSide = Mathf.Min(inner.h.x, inner.h.y) * 2f * 0.7f;
                var size = new Vector2(Mathf.Min(plateauSize.x * 0.5f, maxSide), maxSide);
                for (int t = 0; t < 10 && size.y > 2f; t++)
                    if (TryPlateau(inner, mi, size, out _)) break;
            }
            break;
        }

        if (placedMain < plateauMin)
            Debug.LogWarning($"[MapGenerator] Плато: поставлено {placedMain} из минимума {plateauMin} — не хватило места. Уменьшите размер/количество плато или проход между ними.", this);

        var group = new GameObject("Плато").transform;
        group.SetParent(root, false);
        float tile = textureTileMeters;

        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            var rot = b.box.Rotation;
            // У куба на каждой грани UV 0..1, поэтому тайлинг считаем от размеров блока, чтобы узор не растягивался.
            var sideMat = GetMaterial(plateauSideColor, Pattern.Dirt, new Vector2(Mathf.Max(b.box.h.x, b.box.h.y) * 2f, b.top - b.bottom) / tile);
            var topMat = GetMaterial(plateauTopColor, Pattern.Grass, b.box.h * 2f / tile);
            var side = CreateCube($"Плато_{i + 1:00}", group, sideMat);
            side.localPosition = new Vector3(b.box.c.x, (b.top + b.bottom) * 0.5f - 0.01f, b.box.c.y);
            side.localRotation = rot;
            side.localScale = new Vector3(b.box.h.x * 2f, b.top - b.bottom, b.box.h.y * 2f);
            // Травяная "крыша" чуть шире стен — как кромка травы у плато в Megabonk.
            var cap = CreateCube($"Плато_{i + 1:00}_Крыша", group, topMat);
            cap.localPosition = new Vector3(b.box.c.x, b.top - 0.15f, b.box.c.y);
            cap.localRotation = rot;
            cap.localScale = new Vector3(b.box.h.x * 2f + 0.3f, 0.3f, b.box.h.y * 2f + 0.3f);
        }

        for (int i = 0; i < ramps.Count; i++)
            BuildRampVisual(ramps[i], i + 1, group);
    }

    /// Ставит плато в области area (на земле, если parent < 0, иначе на крыше blocks[parent]) хотя бы с одним подъёмом.
    bool TryPlateau(Obb area, int parent, Vector2 sizeRange, out int index)
    {
        index = -1;
        var box = new Obb
        {
            h = new Vector2(Range(sizeRange), Range(sizeRange)) * 0.5f,
            ax = Dir(Range(-plateauRotation, plateauRotation)),
            c = area.Point(Range(-1f, 1f), Range(-1f, 1f)),
        };
        if (!box.InsideOf(area)) return false;
        if (parent < 0 && Collides(box, plateauGap, plateauGap)) return false;

        float floor, bottom;
        if (parent >= 0) { floor = blocks[parent].top; bottom = floor - 0.5f; }
        else { var span = TerrainSpan(box); floor = span.x; bottom = span.y - 1f; }
        var candidate = new Block { box = box, top = floor + Range(plateauHeight), bottom = bottom };

        var newRamps = new List<Ramp>();
        int want = rng.Next(1, rampsPerPlateauMax + 1);
        for (int k = 0; k < want; k++)
        for (int tries = 0; tries < 15; tries++)
            if (TryRamp(candidate, parent, area, newRamps, out var ramp)) { newRamps.Add(ramp); break; }
        if (newRamps.Count == 0) return false;

        blocks.Add(candidate);
        ramps.AddRange(newRamps);
        index = blocks.Count - 1;
        return true;
    }

    /// Пристройка той же высоты, торчащая из стороны плато: получается Г- или Т-образная форма.
    void TryExtension(Obb area, int mainIndex)
    {
        var main = blocks[mainIndex];
        for (int attempt = 0; attempt < 10; attempt++)
        {
            bool alongX = rng.Next(2) == 0;
            float sign = rng.Next(2) == 0 ? 1f : -1f;
            var h = new Vector2(Range(plateauSize) * 0.3f, Range(plateauSize) * 0.3f);
            float mainAlong = alongX ? main.box.h.x : main.box.h.y;
            float mainAcross = alongX ? main.box.h.y : main.box.h.x;
            float extAlong = alongX ? h.x : h.y;
            float extAcross = alongX ? h.y : h.x;
            if (extAcross >= mainAcross) continue;
            // Пристройка торчит наружу на 40–100% своей длины и сдвинута вдоль стороны случайно.
            float out_ = mainAlong + extAlong * Range(-0.2f, 1f);
            float shift = Range(-(mainAcross - extAcross), mainAcross - extAcross);
            var axAlong = alongX ? main.box.ax : main.box.Az;
            var axAcross = alongX ? main.box.Az : main.box.ax;
            var ext = new Obb { c = main.box.c + axAlong * (out_ * sign) + axAcross * shift, h = h, ax = main.box.ax };
            if (!ext.InsideOf(area) || Collides(ext, plateauGap, 1.5f, mainIndex)) continue;
            blocks.Add(new Block { box = ext, top = main.top, bottom = TerrainSpan(ext).y - 1f });
            return;
        }
    }

    bool TryRamp(Block b, int parent, Obb area, List<Ramp> pending, out Ramp ramp)
    {
        ramp = default;
        float tan = Mathf.Tan(Range(rampAngle) * Mathf.Deg2Rad);
        float width = Range(rampWidth);
        double roll = rng.NextDouble();
        var kind = roll < stairsChance ? RampKind.Stairs : roll < stairsChance + alongWallChance ? RampKind.AlongWall : RampKind.Slab;

        // Сторона плато: внешняя нормаль, направление вдоль стороны, расстояние от центра, половина длины стороны.
        int side = rng.Next(4);
        var nrm = side < 2 ? b.box.ax * (side == 0 ? 1f : -1f) : b.box.Az * (side == 2 ? 1f : -1f);
        var tng = side < 2 ? b.box.Az : b.box.ax;
        float dist = side < 2 ? b.box.h.x : b.box.h.y;
        float halfSide = side < 2 ? b.box.h.y : b.box.h.x;

        Vector2 top, dir, exit;
        float landing = 0f;
        if (kind == RampKind.AlongWall)
        {
            // Пандус идёт вдоль стены, наверху ровная площадка, с неё шаг вбок на плато.
            landing = width;
            if (halfSide * 2f < landing + 1f) return false;
            float sign = rng.Next(2) == 0 ? 1f : -1f;
            float tTop = sign > 0 ? Range(-halfSide + 0.5f, halfSide - landing - 0.5f) : Range(-halfSide + landing + 0.5f, halfSide - 0.5f);
            dir = tng * sign;
            top = b.box.c + nrm * (dist + width * 0.5f) + tng * tTop;
            exit = -nrm;
        }
        else
        {
            if (halfSide * 2f < width + 1f) return false;
            top = b.box.c + nrm * dist + tng * Range(-halfSide + width * 0.5f + 0.5f, halfSide - width * 0.5f - 0.5f);
            dir = -nrm;
            exit = dir;
        }

        float footH;
        if (parent >= 0) footH = blocks[parent].top;
        else
        {
            // Высота подножия зависит от длины склона, а длина — от высоты: двух итераций хватает.
            footH = MeshHeight(top.x, top.y);
            for (int it = 0; it < 2; it++)
            {
                var f = top - dir * ((b.top - footH) / tan);
                footH = MeshHeight(f.x, f.y);
            }
        }
        float length = (b.top - footH) / tan;
        if (length <= 0.5f) return false;

        ramp = new Ramp
        {
            kind = kind, foot = top - dir * length, dir = dir, exit = exit, length = length, landing = landing,
            width = width, footH = footH, topH = b.top, stepHeight = stairStepHeight,
        };
        var box = ramp.Box;
        if (!box.InsideOf(area)) return false;
        // Подножию нужен свободный подход, поэтому запас от плато больше, чем от подъёмов.
        return !Collides(box, 1.5f, 1f, parent, -1, pending);
    }

    void BuildRampVisual(Ramp r, int number, Transform group)
    {
        // Каждый подъём своего оттенка.
        float tint = Range(0.8f, 1.2f);
        var color = new Color(Mathf.Clamp01(rampColor.r * tint), Mathf.Clamp01(rampColor.g * tint), Mathf.Clamp01(rampColor.b * Range(0.8f, 1.2f)));
        var mat = r.kind == RampKind.Stairs
            ? GetMaterial(color, Pattern.Bricks, new Vector2(r.width / textureTileMeters, 1f))
            : GetMaterial(color, Pattern.Stone, new Vector2(r.width, r.Total) / textureTileMeters);
        var dir3 = new Vector3(r.dir.x, 0f, r.dir.y);
        var p0 = new Vector3(r.foot.x, r.footH, r.foot.y);
        var top = r.Top;
        var p1 = new Vector3(top.x, r.topH, top.y);

        if (r.kind == RampKind.Stairs)
        {
            var parent = new GameObject($"Лестница_{number:00}").transform;
            parent.SetParent(group, false);
            parent.localPosition = p0;
            parent.localRotation = Quaternion.LookRotation(dir3);
            int n = r.Steps;
            float depth = r.length / n, rise = (r.topH - r.footH) / n;
            for (int i = 0; i < n; i++)
            {
                // Ступень — блок от пола до своей высоты, чтобы под лестницей не было дыр.
                float h = (i + 1) * rise + 1f;
                var s = CreateCube($"Ступень_{i + 1:00}", parent, mat);
                s.localPosition = new Vector3(0f, (i + 1) * rise - h * 0.5f, (i + 0.5f) * depth);
                s.localScale = new Vector3(r.width, h, depth + 0.02f);
            }
            return;
        }

        const float thick = 0.6f, extra = 0.6f;
        var rot = Quaternion.LookRotation(p1 - p0, Vector3.up);
        var slab = CreateCube(r.kind == RampKind.AlongWall ? $"Пандус_у_стены_{number:00}" : $"Пандус_{number:00}", group, mat);
        slab.localRotation = rot;
        // Плита удлинена только вниз (уходит в землю), верхний край ровно у кромки.
        slab.localPosition = (p0 + p1) * 0.5f - rot * Vector3.forward * (extra * 0.5f) - rot * Vector3.up * (thick * 0.5f);
        slab.localScale = new Vector3(r.width, thick, Vector3.Distance(p0, p1) + extra);

        if (r.landing > 0f)
        {
            var mid = top + r.dir * (r.landing * 0.5f);
            float bottom = Mathf.Min(MeshHeight(mid.x, mid.y), r.footH) - 0.5f;
            var pad = CreateCube($"Пандус_у_стены_{number:00}_Площадка", group, mat);
            pad.localRotation = Quaternion.LookRotation(dir3);
            pad.localPosition = new Vector3(mid.x, (r.topH + bottom) * 0.5f, mid.y);
            pad.localScale = new Vector3(r.width, r.topH - bottom, r.landing);
        }
    }

    Transform CreateCube(string name, Transform parent, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    /// Точка в пределах margin от плато (стена или крыша) — сюда нельзя ставить то, что должно стоять на земле.
    bool NearStructure(Vector2 p, float margin)
    {
        foreach (var b in blocks)
            if (b.box.Contains(p, margin)) return true;
        return false;
    }

    bool NearRamp(Vector2 p, float margin)
    {
        foreach (var r in ramps)
            if (!float.IsNaN(r.HeightAt(p, margin))) return true;
        return false;
    }

    void BuildWalls(Transform root)
    {
        if (wallHeight <= 0f) return;
        const float thick = 2f;
        float maxTop = MaxTerrainHeight;
        foreach (var b in blocks) maxTop = Mathf.Max(maxTop, b.top);
        float h = maxTop + wallHeight + 1f; // от -1 до самой высокой точки карты + wallHeight
        float y = h * 0.5f - 1f;
        var mat = GetMaterial(wallColor, Pattern.Stone, new Vector2(Mathf.Max(mapSize.x, mapSize.y), h) / (textureTileMeters * 2f));
        void Wall(string name, Vector3 pos, Vector3 scale)
        {
            var w = CreateCube(name, root, mat);
            w.localPosition = pos;
            w.localScale = scale;
        }
        float hx = mapSize.x * 0.5f, hz = mapSize.y * 0.5f;
        Wall("Wall_North", new Vector3(0, y, hz + thick * 0.5f), new Vector3(mapSize.x + thick * 2, h, thick));
        Wall("Wall_South", new Vector3(0, y, -hz - thick * 0.5f), new Vector3(mapSize.x + thick * 2, h, thick));
        Wall("Wall_East", new Vector3(hx + thick * 0.5f, y, 0), new Vector3(thick, h, mapSize.y));
        Wall("Wall_West", new Vector3(-hx - thick * 0.5f, y, 0), new Vector3(thick, h, mapSize.y));
    }

    // ---------- Детали рельефа ----------

    readonly List<Vector3> ponds = new List<Vector3>(); // (центр X, центр Z, радиус воды)

    bool InWater(Vector2 p, float margin)
    {
        foreach (var w in ponds)
            if (Vector2.Distance(p, new Vector2(w.x, w.y)) < w.z + margin) return true;
        return false;
    }

    /// Пруды в низинах и каменные осыпи у подножий плато.
    /// Свой генератор случайных чисел: расстановка плато и подъёмов для того же сида не меняется.
    void BuildTerrainDetails(Transform root)
    {
        ponds.Clear();
        var mainRng = rng;
        rng = new System.Random(seed ^ 0xDE7A11);
        var group = new GameObject("Детали рельефа").transform;
        group.SetParent(root, false);

        // Пруды: вода на дне котловины. Уровень — чуть ниже самой низкой точки берега, чтобы край диска уходил в землю.
        var waterMat = new Material(GetMaterial(waterColor)) { name = "Gen_Water" };
        generatedAssets.Add(waterMat);
        if (waterMat.HasProperty("_Smoothness")) waterMat.SetFloat("_Smoothness", 0.92f);
        int pondN = 0;
        foreach (var f in reliefFeatures)
        {
            if (f.w >= 0f || rng.NextDouble() > pondChance) continue;
            var c = new Vector2(f.x, f.y);
            float waterR = f.z * 0.42f;
            if (NearRamp(c, waterR + 2f)) continue;
            bool underPlateau = false;
            foreach (var b in blocks) if (b.box.Contains(c, waterR)) { underPlateau = true; break; }
            if (underPlateau) continue;

            float rim = float.MaxValue;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                rim = Mathf.Min(rim, MeshHeight(c.x + Mathf.Cos(a) * waterR, c.y + Mathf.Sin(a) * waterR));
            }
            float level = rim - 0.05f;
            if (level - MeshHeight(c.x, c.y) < 0.3f) continue; // слишком мелко — не пруд

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = $"Пруд_{++pondN:00}";
            water.transform.SetParent(group, false);
            water.transform.localPosition = new Vector3(c.x, level, c.y);
            water.transform.localScale = new Vector3(waterR * 2f, 0.01f, waterR * 2f);
            water.GetComponent<Renderer>().sharedMaterial = waterMat;
            Kill(water.GetComponent<Collider>()); // мелкая вода: игрок ходит по дну
            ponds.Add(new Vector3(c.x, c.y, waterR));
        }

        // Осыпи: несколько глыб вплотную к стенам плато (не на пандусах и не в воде).
        var rockCat = new SpawnCategory { look = Look.Boulder, color = baseRockColor };
        for (int bi = 0; bi < blocks.Count; bi++)
        {
            var b = blocks[bi];
            if (b.bottom > MeshHeight(b.box.c.x, b.box.c.y)) continue; // второй ярус стоит на крыше — осыпь не нужна
            int n = rng.Next(rocksAtPlateauBase.x, Mathf.Max(rocksAtPlateauBase.x, rocksAtPlateauBase.y) + 1);
            for (int k = 0; k < n; k++)
            for (int attempt = 0; attempt < 8; attempt++)
            {
                // Точка на периметре плато, чуть снаружи стены.
                int side = rng.Next(4);
                float u = side < 2 ? (side == 0 ? 1f : -1f) : Range(-1f, 1f);
                float v = side < 2 ? Range(-1f, 1f) : (side == 2 ? 1f : -1f);
                var edge = b.box.Point(u, v);
                var outward = (edge - b.box.c).normalized;
                var size = new Vector3(Range(0.8f, 2.2f), Range(0.6f, 1.6f), Range(0.8f, 2f));
                var p = edge + outward * (size.x * 0.4f);
                if (NearRamp(p, 2.5f) || InWater(p, 1f) || GroundHeight(p.x, p.y) > MeshHeight(p.x, p.y) + 0.1f) continue;
                if (!IsFree(p, size.x * 0.45f)) continue; // камни осыпи не налезают друг на друга
                var rock = BuildLook(rockCat, size);
                rock.name = $"Осыпь_{bi + 1:00}_{k + 1}";
                rock.transform.SetParent(group, false);
                rock.transform.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                PlaceOnGround(rock.transform, p);
                SnapPendingParts();
                occupied.Add(new Vector3(p.x, size.x * 0.45f, p.y));
                break;
            }
        }
        rng = mainRng;
    }

    /// Детали поверх готовой карты: тропинки, лианы, ящики/бочки и фонари у магазинов.
    /// Строятся последними и своим генератором случайных чисел — уже расставленное не сдвигается.
    void BuildFinishingDetails(Transform root)
    {
        var mainRng = rng;
        rng = new System.Random(seed ^ 0xC0FFEE);
        pathRuns.Clear();
        var group = new GameObject("Отделка").transform;
        group.SetParent(root, false);

        var shops = Positions(root, shopCategory);
        var tables = Positions(root, upgradeCategory);
        var spawn = new Vector2(PlayerSpawnPoint.x, PlayerSpawnPoint.z);

        // Тропинки: спавн -> каждый магазин, магазин -> ближайший стол апгрейда.
        if (paths)
        {
            var pathMat = GetMaterial(pathColor, Pattern.Grass, Vector2.one);
            int n = 0;
            foreach (var s in shops)
            {
                BuildPath(group, spawn, s, pathMat, ++n);
                Vector2? nearest = null;
                foreach (var t in tables)
                    if (nearest == null || (t - s).sqrMagnitude < (nearest.Value - s).sqrMagnitude) nearest = t;
                if (nearest != null) BuildPath(group, s, nearest.Value, pathMat, ++n);
            }
        }

        // Лианы: свисают с кромки крыши плато по стенам.
        var vineGroup = new GameObject("Лианы").transform;
        vineGroup.SetParent(group, false);
        foreach (var b in blocks)
        {
            for (int side = 0; side < 4; side++)
            {
                var nrm = side < 2 ? b.box.ax * (side == 0 ? 1f : -1f) : b.box.Az * (side == 2 ? 1f : -1f);
                float sideLen = 2f * (side < 2 ? b.box.h.y : b.box.h.x);
                int count = Mathf.RoundToInt(sideLen / 10f * vinesPer10m * Range(0.5f, 1.5f));
                for (int k = 0; k < count; k++)
                {
                    float u = side < 2 ? (side == 0 ? 1f : -1f) : Range(-0.9f, 0.9f);
                    float v = side < 2 ? Range(-0.9f, 0.9f) : (side == 2 ? 1f : -1f);
                    var edge = b.box.Point(u, v);
                    if (NearRamp(edge, 1.5f)) continue;
                    // Под стеной может быть не земля, а крыша нижнего плато (второй ярус) или пристройка.
                    float ground = GroundHeight(edge.x + nrm.x * 0.6f, edge.y + nrm.y * 0.6f);
                    float len = Mathf.Min(Range(1f, 4f), b.top - ground - 0.5f);
                    if (len < 0.6f) continue;
                    float w = Range(0.3f, 0.9f);
                    var vine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    vine.name = "Лиана";
                    vine.transform.SetParent(vineGroup, false);
                    vine.transform.localRotation = Quaternion.LookRotation(new Vector3(nrm.x, 0f, nrm.y));
                    vine.transform.localPosition = new Vector3(edge.x + nrm.x * 0.06f, b.top - 0.1f - len * 0.5f, edge.y + nrm.y * 0.06f);
                    vine.transform.localScale = new Vector3(w, len, 0.08f);
                    vine.GetComponent<Renderer>().sharedMaterial = GetMaterial(vineColor * new[] { 0.85f, 1f, 1.15f }[rng.Next(3)], Pattern.Leaves, Vector2.one);
                    Kill(vine.GetComponent<Collider>());
                }
            }
        }

        // Ящики/бочки и фонари вокруг магазинов и столов апгрейда, фонари иногда у руин.
        var crate = new SpawnCategory { look = Look.Crate, color = new Color(0.62f, 0.46f, 0.28f) };
        var barrel = new SpawnCategory { look = Look.Barrel, color = new Color(0.5f, 0.33f, 0.2f) };
        var lantern = new SpawnCategory { look = Look.Lantern, color = lanternLightColor };
        // Радиус самого магазина/стола/руин по их категории: реквизит ставится снаружи, а не впритык и не внутри.
        float CategoryRadius(string name, float extra)
        {
            var cat = categories.Find(x => x.name == name);
            if (cat == null) return 3f;
            if (cat.prefab != null) return cat.clearRadius * 0.5f;
            float r = new Vector2(cat.size.x, cat.size.z).magnitude * 0.5f * cat.scaleRange.y;
            return cat.look == Look.Ruin ? cat.size.x * 0.5f * cat.scaleRange.y + extra : r;
        }
        float shopR = CategoryRadius(shopCategory, 0f), tableR = CategoryRadius(upgradeCategory, 0f), ruinR = CategoryRadius(ruinCategory, 1.5f);
        void Around(Vector2 center, float minDist, float maxDist, SpawnCategory cat, Vector3 size, string name)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range(minDist, maxDist);
                float r = Mathf.Max(size.x, size.z) * 0.7f;
                // Свободная зона самого магазина/стола не в счёт: реквизит ставится как раз внутри неё.
                if (!IsFreeExcept(p, r, center) || NearRamp(p, 2f) || InWater(p, r)) continue;
                if (Mathf.Abs(GroundHeight(p.x, p.y) - GroundHeight(center.x, center.y)) > 1f) continue; // на том же уровне, а не под обрывом
                var go = BuildLook(cat, size);
                go.name = name;
                go.transform.SetParent(group, false);
                go.transform.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                PlaceOnGround(go.transform, p);
                occupied.Add(new Vector3(p.x, r, p.y));
                return;
            }
        }
        foreach (var s in shops)
        {
            int n = rng.Next(propsAroundShops.x, Mathf.Max(propsAroundShops.x, propsAroundShops.y) + 1);
            for (int i = 0; i < n; i++)
            {
                if (rng.NextDouble() < 0.55) Around(s, shopR + 1.2f, shopR + 3.5f, crate, Vector3.one * Range(0.9f, 1.3f), "Ящик");
                else Around(s, shopR + 1.2f, shopR + 3.5f, barrel, new Vector3(0.8f, 1.1f, 0.8f) * Range(0.9f, 1.15f), "Бочка");
            }
            if (lanterns) for (int i = 0; i < 2; i++) Around(s, shopR + 0.8f, shopR + 2.5f, lantern, new Vector3(0.4f, 2.6f, 0.4f), "Фонарь");
        }
        foreach (var t in tables)
        {
            if (rng.NextDouble() < 0.6) Around(t, tableR + 1f, tableR + 2.5f, crate, Vector3.one * Range(0.8f, 1.1f), "Ящик");
            if (lanterns) Around(t, tableR + 0.8f, tableR + 2f, lantern, new Vector3(0.4f, 2.4f, 0.4f), "Фонарь");
        }
        if (lanterns)
            foreach (var r in Positions(root, ruinCategory))
                if (rng.NextDouble() < 0.3) Around(r, ruinR + 0.8f, ruinR + 2.5f, lantern, new Vector3(0.4f, 2.4f, 0.4f), "Фонарь");

        rng = mainRng;
    }

    readonly List<List<(Vector2 p, Vector2 t)>> pathRuns = new List<List<(Vector2 p, Vector2 t)>>(); // непрерывные участки тропинок

    /// Место подходит для декора: свободно, не на подъёме и не в воде, земля ровная в пределах maxSlope.
    bool DecorSpot(Vector2 p, float r, float maxSlope)
    {
        float m = edgeMargin * 0.5f;
        if (Mathf.Abs(p.x) > mapSize.x * 0.5f - m || Mathf.Abs(p.y) > mapSize.y * 0.5f - m) return false;
        return IsFree(p, r) && !NearRamp(p, r + 1f) && !InWater(p, r) && SlopeAngle(p.x, p.y) <= maxSlope;
    }

    GameObject PlaceDecor(Look look, Color color, Vector3 size, Vector2 p, Transform parent, string name, float yaw, float occupyRadius)
    {
        var go = BuildLook(new SpawnCategory { look = look, color = color }, size);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        PlaceOnGround(go.transform, p);
        SnapPendingParts();
        if (occupyRadius > 0f) occupied.Add(new Vector3(p.x, occupyRadius, p.y));
        return go;
    }

    /// Окружение: костры, заборчики вдоль тропинок, указатель у спавна, скальные выходы на курганах,
    /// цветочные поляны, зелень вдоль стен-границ, каменные арки. Последний слой, свой генератор случайных чисел.
    void BuildSurroundings(Transform root)
    {
        var mainRng = rng;
        rng = new System.Random(seed ^ 0xBEEF);
        var group = new GameObject("Окружение").transform;
        group.SetParent(root, false);
        float tile = textureTileMeters;
        Vector2 RandomPoint(float margin) => new Vector2(Range(MapMin.x + margin, -MapMin.x - margin), Range(MapMin.y + margin, -MapMin.y - margin));

        // Костры: у руин, если есть, иначе где придётся.
        var ruins = Positions(root, ruinCategory);
        int fires = rng.Next(campfireCount.x, Mathf.Max(campfireCount.x, campfireCount.y) + 1);
        for (int i = 0; i < fires; i++)
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector2 p;
            if (ruins.Count > 0 && attempt < 12)
            {
                float a = Range(0f, Mathf.PI * 2f);
                p = ruins[rng.Next(ruins.Count)] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range(4f, 6f);
            }
            else p = RandomPoint(edgeMargin);
            if (!DecorSpot(p, 2.2f, 12f)) continue;
            PlaceDecor(Look.Campfire, new Color(1f, 0.45f, 0.1f), new Vector3(1.6f, 0.6f, 1.6f), p, group, "Костёр", Range(0f, 360f), 2.2f);
            // Бревно-скамейка рядом.
            float sa = Range(0f, Mathf.PI * 2f);
            var seat = p + new Vector2(Mathf.Cos(sa), Mathf.Sin(sa)) * 2f;
            if (DecorSpot(seat, 0.8f, 15f))
                PlaceDecor(Look.Log, woodColor * 0.85f, new Vector3(2f, 0.45f, 0.45f), seat, group, "Скамья", -sa * Mathf.Rad2Deg + 90f, 0.8f);
            break;
        }

        // Заборчики: столбики через ~2 м с перекладинами вдоль одной стороны тропинки.
        var fenceMat = GetMaterial(woodColor, Pattern.Bark, Vector2.one);
        foreach (var run in pathRuns)
        {
            if (run.Count < 10 || rng.NextDouble() > fenceOnPathChance) continue;
            float sideSign = rng.Next(2) == 0 ? 1f : -1f;
            int start = rng.Next(0, run.Count / 2);
            int maxLength = Mathf.Min(run.Count - start, 30);
            if (maxLength <= 10) continue; // остаток участка слишком короткий для забора
            int length = rng.Next(10, maxLength + 1);
            var fence = new GameObject("Забор").transform;
            fence.SetParent(group, false);
            Vector3? prevTop = null;
            for (int i = start; i < start + length; i += 3) // точки тропинки через 0.6 м, столбик через ~1.8 м
            {
                var (p, t) = run[i];
                var post = p + new Vector2(-t.y, t.x) * sideSign * (pathWidth * 0.5f + 0.7f);
                // Столбик и перекладина к нему не должны заходить в стену плато (тропинка часто идёт вдоль стены).
                bool hitsStructure = NearStructure(post, 0.4f)
                    || (prevTop != null && NearStructure((post + new Vector2(prevTop.Value.x, prevTop.Value.z)) * 0.5f, 0.3f));
                if (hitsStructure || !IsFree(post, 0.3f) || NearRamp(post, 1f) || InWater(post, 0.3f)) { prevTop = null; continue; }
                float y = MeshHeight(post.x, post.y);
                var postGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                postGo.name = "Столбик";
                postGo.transform.SetParent(fence, false);
                postGo.transform.localPosition = new Vector3(post.x, y + 0.5f, post.y);
                postGo.transform.localScale = new Vector3(0.16f, 1.1f, 0.16f);
                postGo.GetComponent<Renderer>().sharedMaterial = fenceMat;
                var top = new Vector3(post.x, y + 0.8f, post.y);
                if (prevTop != null && rng.NextDouble() > 0.12) // изредка перекладина выпала
                {
                    var a = prevTop.Value; var mid = (a + top) * 0.5f;
                    foreach (float dy in new[] { 0f, -0.35f })
                    {
                        var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        rail.name = "Перекладина";
                        rail.transform.SetParent(fence, false);
                        rail.transform.localPosition = mid + Vector3.up * dy;
                        rail.transform.localRotation = Quaternion.LookRotation(top - a);
                        rail.transform.localScale = new Vector3(0.08f, 0.12f, Vector3.Distance(a, top) + 0.1f);
                        rail.GetComponent<Renderer>().sharedMaterial = fenceMat;
                    }
                }
                prevTop = top;
                occupied.Add(new Vector3(post.x, 0.3f, post.y));
            }
            if (fence.childCount < 2) Kill(fence.gameObject); // одиночный столбик или пусто — не забор
        }

        // Указатель у спавна: таблички смотрят в сторону магазинов.
        if (spawnSignpost)
        {
            var spawn = new Vector2(PlayerSpawnPoint.x, PlayerSpawnPoint.z);
            var shops = Positions(root, shopCategory);
            for (int attempt = 0; attempt < 12 && shops.Count > 0; attempt++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                var p = spawn + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range(3f, 5f);
                // Свободная зона игрока не в счёт: указатель стоит как раз рядом со спавном.
                if (!IsFreeExcept(p, 0.6f, spawn) || NearRamp(p, 1.5f) || InWater(p, 0.6f) || SlopeAngle(p.x, p.y) > 20f) continue;
                var sign = new GameObject("Указатель").transform;
                sign.SetParent(group, false);
                float y = GroundHeight(p.x, p.y);
                sign.localPosition = new Vector3(p.x, y, p.y);
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.transform.SetParent(sign, false);
                post.transform.localPosition = new Vector3(0f, 1.3f, 0f);
                post.transform.localScale = new Vector3(0.18f, 2.6f, 0.18f);
                post.GetComponent<Renderer>().sharedMaterial = fenceMat;
                int k = 0;
                foreach (var s in shops)
                {
                    if (k >= 3) break;
                    var d = s - p;
                    var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plank.name = "Табличка";
                    plank.transform.SetParent(sign, false);
                    plank.transform.localRotation = Quaternion.LookRotation(new Vector3(d.x, 0f, d.y)) * Quaternion.Euler(0f, 90f, 0f);
                    plank.transform.localPosition = new Vector3(0f, 2.3f - k * 0.45f, 0f) + new Vector3(d.x, 0f, d.y).normalized * 0.55f;
                    plank.transform.localScale = new Vector3(1.2f, 0.32f, 0.07f);
                    plank.GetComponent<Renderer>().sharedMaterial = GetMaterial(woodColor * 1.15f, Pattern.Bark, Vector2.one);
                    Kill(plank.GetComponent<Collider>());
                    k++;
                }
                occupied.Add(new Vector3(p.x, 0.6f, p.y));
                break;
            }
        }

        // Скальные выходы на вершинах курганов.
        var rockColor = new Color(0.55f, 0.54f, 0.52f);
        foreach (var f in reliefFeatures)
        {
            if (f.w <= 0f || rng.NextDouble() > outcropChance) continue;
            int n = rng.Next(3, 7);
            for (int i = 0; i < n; i++)
            for (int attempt = 0; attempt < 6; attempt++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                var p = new Vector2(f.x, f.y) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range(0f, f.z * 0.35f);
                var size = new Vector3(Range(1.5f, 3.5f), Range(1.2f, 3f), Range(1.5f, 3f));
                if (!DecorSpot(p, size.x * 0.5f, 35f) || GroundHeight(p.x, p.y) > MeshHeight(p.x, p.y) + 0.1f) continue;
                PlaceDecor(Look.Boulder, rockColor * Range(0.9f, 1.08f), size, p, group, "Скальный выход", Range(0f, 360f), size.x * 0.5f);
                break;
            }
        }

        // Цветочные поляны: десятки цветков одного-двух цветов на ровном месте.
        var flowerColors = new[] { new Color(1f, 0.95f, 0.85f), new Color(1f, 0.82f, 0.2f), new Color(0.95f, 0.45f, 0.7f), new Color(0.6f, 0.5f, 1f), new Color(1f, 0.4f, 0.3f) };
        var stemMat = GetMaterial(new Color(0.28f, 0.5f, 0.22f));
        int meadows = rng.Next(flowerMeadows.x, Mathf.Max(flowerMeadows.x, flowerMeadows.y) + 1);
        for (int m = 0; m < meadows; m++)
        for (int attempt = 0; attempt < 15; attempt++)
        {
            var c = RandomPoint(edgeMargin);
            float rad = Range(3f, 6f);
            if (SlopeAngle(c.x, c.y) > 10f || NearRamp(c, rad + 1f) || InWater(c, rad) || GroundHeight(c.x, c.y) > MeshHeight(c.x, c.y) + 0.1f) continue;
            var meadow = new GameObject("Цветочная поляна").transform;
            meadow.SetParent(group, false);
            var main = flowerColors[rng.Next(flowerColors.Length)];
            var second = flowerColors[rng.Next(flowerColors.Length)];
            int count = rng.Next(20, 40);
            for (int i = 0; i < count; i++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad * Mathf.Sqrt(Range(0f, 1f));
                if (NearRamp(p, 0.5f) || GroundHeight(p.x, p.y) > MeshHeight(p.x, p.y) + 0.1f) continue;
                float y = MeshHeight(p.x, p.y);
                float h = Range(0.25f, 0.5f);
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stem.transform.SetParent(meadow, false);
                stem.transform.localPosition = new Vector3(p.x, y + h * 0.5f, p.y);
                stem.transform.localScale = new Vector3(0.06f, h, 0.06f);
                stem.GetComponent<Renderer>().sharedMaterial = stemMat;
                Kill(stem.GetComponent<Collider>());
                var bloom = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bloom.transform.SetParent(meadow, false);
                bloom.transform.localPosition = new Vector3(p.x, y + h + 0.08f, p.y);
                bloom.transform.localRotation = Quaternion.Euler(0f, Range(0f, 90f), 0f);
                bloom.transform.localScale = Vector3.one * Range(0.16f, 0.26f);
                bloom.GetComponent<Renderer>().sharedMaterial = GetMaterial(rng.NextDouble() < 0.75 ? main : second);
                Kill(bloom.GetComponent<Collider>());
            }
            break;
        }

        // Зелень вдоль стен-границ: кусты и камни смягчают резкую линию стены.
        if (borderVegetation)
        {
            var border = new GameObject("Зелень у стен").transform;
            border.SetParent(group, false);
            var bushColor = new Color(0.2f, 0.42f, 0.18f);
            float hx = mapSize.x * 0.5f, hz = mapSize.y * 0.5f;
            for (int side = 0; side < 4; side++)
            {
                float len = side < 2 ? mapSize.x : mapSize.y;
                for (float s = -len * 0.5f + 2f; s < len * 0.5f - 2f; s += Range(4f, 8f))
                {
                    bool bush = rng.NextDouble() < 0.7;
                    var size = bush ? new Vector3(2f, 1.4f, 2f) * Range(0.8f, 1.5f) : new Vector3(Range(1.5f, 3f), Range(1f, 2.2f), Range(1.5f, 3f));
                    // Отступ от стены — не меньше полудиагонали объекта (он поворачивается), иначе он влезает в стену.
                    // У глыбы рядом может лежать обломок — её габарит ~ max(size) от центра.
                    float reach = bush ? new Vector2(size.x, size.z).magnitude * 0.5f : Mathf.Max(size.x, size.z) * 1.05f;
                    float inset = reach + Range(0.3f, 1.5f);
                    var p = side == 0 ? new Vector2(s, hz - inset) : side == 1 ? new Vector2(s, -hz + inset)
                          : side == 2 ? new Vector2(hx - inset, s) : new Vector2(-hx + inset, s);
                    if (!IsFree(p, 1f) || NearRamp(p, 2f) || InWater(p, 1f)) continue;
                    if (bush) PlaceDecor(Look.Bush, bushColor, size, p, border, "Куст у стены", Range(0f, 360f), 1f);
                    else PlaceDecor(Look.Boulder, rockColor, size, p, border, "Камень у стены", Range(0f, 360f), 1.2f);
                }
            }
        }

        // Каменные арки: две колонны и перекладина, иногда полуразрушенная.
        int arches = rng.Next(archCount.x, Mathf.Max(archCount.x, archCount.y) + 1);
        for (int i = 0; i < arches; i++)
        for (int attempt = 0; attempt < 25; attempt++)
        {
            var p = RandomPoint(edgeMargin + 4f);
            if (!DecorSpot(p, 4f, 8f) || GroundHeight(p.x, p.y) > MeshHeight(p.x, p.y) + 0.1f) continue;
            PlaceDecor(Look.Arch, new Color(0.62f, 0.6f, 0.56f), new Vector3(5f, 5f, 1.2f), p, group, "Арка", Range(0f, 180f), 4f);
            break;
        }

        rng = mainRng;
    }

    /// Куб-деталь без коллайдера (для декора, который потом склеивается).
    Transform DecorCube(Transform parent, Material mat, Vector3 localPos, Vector3 scale, Quaternion localRot, string name = "Деталь")
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Kill(go.GetComponent<Collider>());
        return go.transform;
    }

    /// Мосты между плато, камыши и кувшинки у прудов, кучки костей, приподнятые рельсы.
    /// Последний слой, свой генератор случайных чисел — уже расставленное не сдвигается.
    void BuildExtras(Transform root)
    {
        var mainRng = rng;
        rng = new System.Random(seed ^ 0xB0A7);
        var group = new GameObject("Дополнения").transform;
        group.SetParent(root, false);
        Vector2 RandomPoint(float margin) => new Vector2(Range(MapMin.x + margin, -MapMin.x - margin), Range(MapMin.y + margin, -MapMin.y - margin));

        // Габариты уже стоящих объектов: мост/рельс не должен проходить сквозь то, что дотягивается до его высоты.
        // Земля, плато, стены, тропинки, вода и лианы не в счёт — их проверяет рельеф или они плоские/на стенах.
        var obstacles = new List<Bounds>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            var t = mr.transform;
            string n = t.name;
            if (n == "Terrain" || n.StartsWith("Wall_") || n.StartsWith("Тропинка") || n.StartsWith("Пруд") || n == "Лиана") continue;
            if (t.parent != null && t.parent.name == "Плато") continue;
            obstacles.Add(mr.bounds);
        }
        bool Blocked(Vector3 p, float radius)
        {
            foreach (var b in obstacles)
                if (p.x > b.min.x - radius && p.x < b.max.x + radius && p.z > b.min.z - radius && p.z < b.max.z + radius
                    && p.y + 0.5f > b.min.y && p.y - 1.2f < b.max.y) return true;
            return false;
        }

        // --- Мосты: от края одного плато до края другого по линии центров ---
        Vector2 Exit(Obb box, Vector2 dir)
        {
            float t = 0f;
            while (t < 300f && box.Contains(box.c + dir * t)) t += 0.25f;
            return box.c + dir * Mathf.Max(0f, t - 0.25f);
        }
        var candidates = new List<(int a, int b, Vector2 ea, Vector2 eb)>();
        for (int i = 0; i < blocks.Count; i++)
        for (int j = i + 1; j < blocks.Count; j++)
        {
            var A = blocks[i]; var B = blocks[j];
            var d = (B.box.c - A.box.c).normalized;
            var ea = Exit(A.box, d); var eb = Exit(B.box, -d);
            float gap = Vector2.Distance(ea, eb);
            if (gap < 3f || gap > 22f || Vector2.Dot(eb - ea, d) <= 0f) continue;
            if (Mathf.Abs(A.top - B.top) > gap * 0.45f) continue; // уклон настила не круче ~24°
            bool ok = true;
            for (float s = 0.5f; s < gap - 0.5f && ok; s += 0.5f)
            {
                var p = ea + d * s;
                float deck = Mathf.Lerp(A.top, B.top, s / gap);
                // Под мостом — пустота: не крыша/холм/подъём и не деревья/постройки, которые проткнут настил.
                if (GroundHeight(p.x, p.y) > deck - 1.5f || NearRamp(p, 1.5f) || Blocked(new Vector3(p.x, deck, p.y), 1.2f)) ok = false;
            }
            // Концы моста заходят на крыши на 0.8 м: там не должно стоять дерево, лут и т.п.
            for (float s = -0.8f; s <= 0f && ok; s += 0.4f)
                if (Blocked(new Vector3(ea.x + d.x * s, A.top + 0.2f, ea.y + d.y * s), 1.2f)) ok = false;
            for (float s = 0f; s <= 0.8f && ok; s += 0.4f)
                if (Blocked(new Vector3(eb.x + d.x * s, B.top + 0.2f, eb.y + d.y * s), 1.2f)) ok = false;
            if (ok) candidates.Add((i, j, ea, eb));
        }
        var bridged = new HashSet<int>();
        int bridgesWanted = rng.Next(bridgeCount.x, Mathf.Max(bridgeCount.x, bridgeCount.y) + 1);
        var deckMat = GetMaterial(woodColor, Pattern.Bark, Vector2.one);
        var ropeMat = GetMaterial(new Color(0.36f, 0.28f, 0.19f));
        for (int k = 0; k < candidates.Count && bridgesWanted > 0; k++)
        {
            var (ia, ib, ea, eb) = candidates[rng.Next(candidates.Count)];
            if (bridged.Contains(ia) || bridged.Contains(ib)) continue;
            bridged.Add(ia); bridged.Add(ib); bridgesWanted--;
            var dir = (eb - ea).normalized;
            const float width = 2.2f, overlap = 0.3f;
            // Настил — ровно от кромки до кромки: на концах он совпадает с крышами по высоте, без ступеньки
            // и без врезания в стену, когда плато разной высоты.
            var p0 = new Vector3(ea.x, blocks[ia].top, ea.y);
            var p1 = new Vector3(eb.x, blocks[ib].top, eb.y);
            float len = Vector3.Distance(p0, p1);
            var bridge = new GameObject("Мост").transform;
            bridge.SetParent(group, false);
            bridge.localPosition = (p0 + p1) * 0.5f;
            bridge.localRotation = Quaternion.LookRotation(p1 - p0);
            // Ровный невидимый настил для физики: игрок не спотыкается о щели между досками.
            var deck = bridge.gameObject.AddComponent<BoxCollider>();
            deck.size = new Vector3(width, 0.2f, len);
            deck.center = new Vector3(0f, -0.1f, 0f);
            for (float z = -len * 0.5f + 0.25f; z < len * 0.5f; z += 0.55f)
                DecorCube(bridge, GetMaterial(woodColor * new[] { 0.85f, 1f, 1.1f }[rng.Next(3)], Pattern.Bark, Vector2.one),
                    new Vector3(0f, -0.06f, z), new Vector3(width + Range(-0.1f, 0.15f), 0.12f, 0.42f), Quaternion.Euler(0f, Range(-3f, 3f), 0f), "Доска");
            foreach (float sx in new[] { -width * 0.5f, width * 0.5f })
            {
                foreach (float z in new[] { -len * 0.5f + overlap * 0.5f, len * 0.5f - overlap * 0.5f })
                    DecorCube(bridge, deckMat, new Vector3(sx, 0.5f, z), new Vector3(0.14f, 1.1f, 0.14f), Quaternion.identity, "Столбик");
                DecorCube(bridge, ropeMat, new Vector3(sx, 0.9f, 0f), new Vector3(0.05f, 0.05f, len - overlap), Quaternion.identity, "Перила");
            }
        }

        // --- Камыши по берегу и кувшинки на воде ---
        if (pondPlants)
        {
            var reedMat = GetMaterial(new Color(0.3f, 0.5f, 0.22f));
            var headMat = GetMaterial(new Color(0.42f, 0.27f, 0.15f));
            var padMat = GetMaterial(new Color(0.25f, 0.55f, 0.25f));
            var lotusMat = GetMaterial(new Color(1f, 0.7f, 0.85f));
            foreach (var w in ponds)
            {
                var c = new Vector2(w.x, w.y);
                var plants = new GameObject("Растения у пруда").transform;
                plants.SetParent(group, false);
                int reeds = rng.Next(10, 20);
                for (int i = 0; i < reeds; i++)
                {
                    float a = Range(0f, Mathf.PI * 2f);
                    var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * w.z * Range(0.85f, 1.05f);
                    float y = MeshHeight(p.x, p.y), h = Range(0.8f, 1.5f);
                    var tilt = Quaternion.Euler(Range(-8f, 8f), 0f, Range(-8f, 8f));
                    DecorCube(plants, reedMat, new Vector3(p.x, y + h * 0.5f, p.y), new Vector3(0.06f, h, 0.06f), tilt, "Камыш");
                    if (rng.NextDouble() < 0.6)
                        DecorCube(plants, headMat, new Vector3(p.x, y + h * 0.85f, p.y), new Vector3(0.11f, 0.22f, 0.11f), tilt, "Камыш");
                }
                float level = 0f; int levelN = 0;
                foreach (Transform t in root.Find("Детали рельефа"))
                    if (t.name.StartsWith("Пруд") && (new Vector2(t.localPosition.x, t.localPosition.z) - c).sqrMagnitude < 0.01f) { level = t.localPosition.y; levelN++; }
                if (levelN == 0) continue;
                int pads = rng.Next(3, 8);
                for (int i = 0; i < pads; i++)
                {
                    float a = Range(0f, Mathf.PI * 2f);
                    var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * w.z * Mathf.Sqrt(Range(0.05f, 0.6f));
                    float r = Range(0.35f, 0.6f);
                    DecorCube(plants, padMat, new Vector3(p.x, level + 0.02f, p.y), new Vector3(r * 2f, 0.03f, r * 2f), Quaternion.Euler(0f, Range(0f, 90f), 0f), "Кувшинка");
                    if (rng.NextDouble() < 0.3)
                        DecorCube(plants, lotusMat, new Vector3(p.x, level + 0.12f, p.y), Vector3.one * 0.2f, Quaternion.Euler(0f, 45f, 0f), "Цветок");
                }
            }
        }

        // --- Кучки костей: череп и несколько костей, чаще у руин ---
        var boneMat = GetMaterial(new Color(0.92f, 0.9f, 0.82f));
        var eyeMat = GetMaterial(new Color(0.15f, 0.13f, 0.12f));
        var ruins = Positions(root, ruinCategory);
        int piles = rng.Next(bonePiles.x, Mathf.Max(bonePiles.x, bonePiles.y) + 1);
        for (int i = 0; i < piles; i++)
        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector2 p;
            if (ruins.Count > 0 && attempt < 8)
            {
                float a = Range(0f, Mathf.PI * 2f);
                p = ruins[rng.Next(ruins.Count)] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range(2.5f, 5f);
            }
            else p = RandomPoint(edgeMargin);
            if (!DecorSpot(p, 1f, 20f)) continue;
            var pile = new GameObject("Кости").transform;
            pile.SetParent(group, false);
            pile.localPosition = new Vector3(p.x, GroundHeight(p.x, p.y), p.y);
            pile.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            DecorCube(pile, boneMat, new Vector3(0f, 0.17f, 0f), new Vector3(0.34f, 0.32f, 0.38f), Quaternion.Euler(Range(-15f, 15f), 0f, Range(-10f, 10f)), "Череп");
            foreach (float ex in new[] { -0.08f, 0.08f })
                DecorCube(pile, eyeMat, new Vector3(ex, 0.2f, 0.18f), new Vector3(0.08f, 0.08f, 0.04f), Quaternion.identity, "Глазница");
            int bones = rng.Next(3, 7);
            for (int b = 0; b < bones; b++)
                DecorCube(pile, boneMat, new Vector3(Range(-0.7f, 0.7f), 0.05f, Range(-0.7f, 0.7f)), new Vector3(0.08f, 0.08f, Range(0.35f, 0.6f)),
                    Quaternion.Euler(0f, Range(0f, 180f), Range(-10f, 10f)), "Кость");
            occupied.Add(new Vector3(p.x, 1f, p.y));
            break;
        }

        // --- Приподнятые рельсы на опорах ---
        var railMat = GetMaterial(railColor, Pattern.None, default, 0.15f);
        var postMat = GetMaterial(new Color(0.3f, 0.3f, 0.33f), Pattern.Stone, Vector2.one);
        int rails = rng.Next(railCount.x, Mathf.Max(railCount.x, railCount.y) + 1);
        for (int i = 0; i < rails; i++)
        for (int attempt = 0; attempt < 30; attempt++)
        {
            var a = RandomPoint(edgeMargin + 5f);
            var b = RandomPoint(edgeMargin + 5f);
            float dist = Vector2.Distance(a, b);
            if (dist < 40f || dist > 90f) continue;
            var ctrl = (a + b) * 0.5f + new Vector2(-(b - a).y, (b - a).x).normalized * dist * Range(-0.3f, 0.3f);
            int n = Mathf.CeilToInt(dist);
            var pts = new List<Vector3>();
            bool ok = true;
            for (int s = 0; s <= n && ok; s++)
            {
                float t = (float)s / n;
                var p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * ctrl + t * t * b;
                // В обход плато, подъёмов, воды и объектов (деревья, столбы, магазины проткнули бы рельс).
                var rp = new Vector3(p.x, MeshHeight(p.x, p.y) + 2.6f, p.y);
                if (GroundHeight(p.x, p.y) > MeshHeight(p.x, p.y) + 0.1f || NearRamp(p, 2f) || InWater(p, 1f) || Blocked(rp, 0.6f)) ok = false;
                else pts.Add(rp);
            }
            if (!ok) continue;
            // Сглаживаем высоту, чтобы рельс шёл плавно, а не повторял каждую кочку.
            for (int pass = 0; pass < 3; pass++)
                for (int s = 1; s < pts.Count - 1; s++)
                    pts[s] = new Vector3(pts[s].x, (pts[s - 1].y + pts[s].y * 2f + pts[s + 1].y) * 0.25f, pts[s].z);
            var rail = new GameObject("Рельс").transform;
            rail.SetParent(group, false);
            for (int s = 1; s < pts.Count; s++)
            {
                var p0 = pts[s - 1]; var p1 = pts[s];
                DecorCube(rail, railMat, (p0 + p1) * 0.5f, new Vector3(0.16f, 0.16f, Vector3.Distance(p0, p1) + 0.02f), Quaternion.LookRotation(p1 - p0), "Рельс");
            }
            for (int s = 0; s < pts.Count; s += 5)
            {
                var p = pts[s];
                float ground = MeshHeight(p.x, p.z);
                float h = p.y - ground;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube); // опора с коллайдером: сквозь неё не пройти
                post.name = "Опора";
                post.transform.SetParent(rail, false);
                post.transform.localPosition = new Vector3(p.x, ground + h * 0.5f - 0.1f, p.z);
                post.transform.localScale = new Vector3(0.28f, h, 0.28f);
                post.GetComponent<Renderer>().sharedMaterial = postMat;
                occupied.Add(new Vector3(p.x, 0.5f, p.z));
            }
            break;
        }

        rng = mainRng;
    }

    /// Простая система частиц (дым, искры, светлячки). Материал — стандартный для частиц у текущего пайплайна.
    void MakeParticles(Transform parent, Vector3 pos, string name, Material mat, Color color, float rate, Vector2 life,
        Vector2 speed, Vector2 size, float radius, float gravity, float noise, int maxParticles)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // конус эмиссии смотрит вверх
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = rate;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = radius;
        // Плавное появление и исчезание.
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = fade;
        if (noise > 0f)
        {
            var n = ps.noise;
            n.enabled = true;
            n.strength = noise;
            n.frequency = 0.5f;
        }
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        ps.Play();
    }

    /// Лагеря и ориентиры: частицы у костров/прудов/полян, палатки у костров, телеги, колодцы, флаги на высоких плато, кладбища.
    /// Последний слой, свой генератор случайных чисел — уже расставленное не сдвигается.
    void BuildCampsAndLandmarks(Transform root)
    {
        var mainRng = rng;
        rng = new System.Random(seed ^ 0xCA4F);
        var group = new GameObject("Лагеря и ориентиры").transform;
        group.SetParent(root, false);
        Vector2 RandomPoint(float margin) => new Vector2(Range(MapMin.x + margin, -MapMin.x - margin), Range(MapMin.y + margin, -MapMin.y - margin));
        Vector2 Near(Vector2 c, float min, float max) { float a = Range(0f, Mathf.PI * 2f); return c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range(min, max); }

        var env = root.Find("Окружение");
        var campfires = new List<Vector3>();
        var meadows = new List<Vector3>();
        if (env != null)
            foreach (Transform t in env)
            {
                if (t.name == "Костёр") campfires.Add(t.localPosition);
                else if (t.name == "Цветочная поляна" && t.childCount > 0) meadows.Add(t.GetChild(0).localPosition);
            }

        // Частицы. Без материала частиц у пайплайна — пропускаем, а не рисуем розовым.
        var particleMat = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultParticleMaterial : null;
        if (ambientParticles && particleMat != null)
        {
            foreach (var f in campfires)
            {
                MakeParticles(group, f + Vector3.up * 0.4f, "Дым", particleMat, new Color(0.55f, 0.55f, 0.55f, 0.35f), 6f,
                    new Vector2(3f, 5f), new Vector2(0.6f, 1.2f), new Vector2(0.6f, 1.4f), 0.3f, -0.02f, 0.3f, 60);
                MakeParticles(group, f + Vector3.up * 0.3f, "Искры", particleMat, new Color(3f, 1.4f, 0.3f, 1f), 10f,
                    new Vector2(0.8f, 1.6f), new Vector2(1f, 2.2f), new Vector2(0.04f, 0.09f), 0.25f, 0f, 0.8f, 40);
            }
            foreach (var w in ponds)
                MakeParticles(group, new Vector3(w.x, MeshHeight(w.x, w.y) + 0.8f, w.y), "Светлячки", particleMat, new Color(1.8f, 2.2f, 0.6f, 1f), 3f,
                    new Vector2(3f, 6f), new Vector2(0.05f, 0.2f), new Vector2(0.06f, 0.11f), w.z, 0f, 0.6f, 25);
            foreach (var m in meadows)
                MakeParticles(group, m + Vector3.up * 0.6f, "Светлячки", particleMat, new Color(1.8f, 2.2f, 0.6f, 1f), 2f,
                    new Vector2(3f, 6f), new Vector2(0.05f, 0.2f), new Vector2(0.06f, 0.11f), 4f, 0f, 0.6f, 15);
        }

        // Палатки у костров.
        if (tentsAtCampfires)
        {
            var tentColors = new[] { new Color(0.75f, 0.3f, 0.25f), new Color(0.3f, 0.45f, 0.7f), new Color(0.8f, 0.72f, 0.5f), new Color(0.4f, 0.55f, 0.3f) };
            foreach (var f in campfires)
            {
                var c = new Vector2(f.x, f.z);
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    var p = Near(c, 4f, 5.5f);
                    if (!DecorSpot(p, 1.8f, 12f)) continue;
                    var d = c - p;
                    // Вход палатки смотрит на костёр.
                    PlaceDecor(Look.Tent, tentColors[rng.Next(tentColors.Length)], new Vector3(2.4f, 1.8f, 2.8f), p, group, "Палатка",
                        Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 1.8f);
                    break;
                }
            }
        }

        // Телеги: у руин или магазинов.
        var anchors = new List<Vector2>(Positions(root, ruinCategory));
        anchors.AddRange(Positions(root, shopCategory));
        int carts = rng.Next(cartCount.x, Mathf.Max(cartCount.x, cartCount.y) + 1);
        for (int i = 0; i < carts; i++)
        for (int attempt = 0; attempt < 15; attempt++)
        {
            var p = anchors.Count > 0 && attempt < 10 ? Near(anchors[rng.Next(anchors.Count)], 4f, 8f) : RandomPoint(edgeMargin);
            if (!DecorSpot(p, 1.8f, 10f)) continue;
            PlaceDecor(Look.Cart, woodColor, new Vector3(1.6f, 0.9f, 2.6f), p, group, "Телега", Range(0f, 360f), 1.8f);
            break;
        }

        // Колодцы: у руин, иначе где придётся.
        var ruins = Positions(root, ruinCategory);
        int wells = rng.Next(wellCount.x, Mathf.Max(wellCount.x, wellCount.y) + 1);
        for (int i = 0; i < wells; i++)
        for (int attempt = 0; attempt < 15; attempt++)
        {
            var p = ruins.Count > 0 && attempt < 10 ? Near(ruins[rng.Next(ruins.Count)], 5f, 9f) : RandomPoint(edgeMargin);
            if (!DecorSpot(p, 1.6f, 10f)) continue;
            PlaceDecor(Look.Well, new Color(0.6f, 0.58f, 0.55f), new Vector3(2f, 2.6f, 2f), p, group, "Колодец", Range(0f, 360f), 1.6f);
            break;
        }

        // Флаги на самых высоких плато — их видно издалека, по ним удобно ориентироваться.
        var order = new List<int>();
        for (int i = 0; i < blocks.Count; i++) order.Add(i);
        order.Sort((a, b) => blocks[b].top.CompareTo(blocks[a].top));
        var flagColors = new[] { new Color(0.85f, 0.2f, 0.2f), new Color(0.95f, 0.8f, 0.2f), new Color(0.25f, 0.45f, 0.9f) };
        var poleMat = GetMaterial(new Color(0.35f, 0.26f, 0.18f), Pattern.Bark, Vector2.one);
        int flags = 0;
        foreach (int bi in order)
        {
            if (flags >= flagsOnHighest) break;
            var b = blocks[bi];
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var p = b.box.Point(Range(-0.6f, 0.6f), Range(-0.6f, 0.6f));
                if (!IsFree(p, 0.6f) || NearRamp(p, 2f) || Mathf.Abs(GroundHeight(p.x, p.y) - b.top) > 0.05f) continue;
                var flag = new GameObject("Флаг").transform;
                flag.SetParent(group, false);
                flag.localPosition = new Vector3(p.x, b.top, p.y);
                flag.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cube); // столб с коллайдером
                pole.name = "Флагшток";
                pole.transform.SetParent(flag, false);
                pole.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                pole.transform.localScale = new Vector3(0.14f, 5f, 0.14f);
                pole.GetComponent<Renderer>().sharedMaterial = poleMat;
                DecorCube(flag, GetMaterial(flagColors[flags % flagColors.Length], Pattern.None, default, 0.3f),
                    new Vector3(0.8f, 4.35f, 0f), new Vector3(1.5f, 0.95f, 0.05f), Quaternion.Euler(0f, 0f, Range(-4f, 4f)), "Полотнище");
                occupied.Add(new Vector3(p.x, 0.6f, p.y));
                flags++;
                break;
            }
        }

        // Кладбище: ряды надгробий и крест, на ровном месте.
        var stoneMat = GetMaterial(new Color(0.58f, 0.57f, 0.55f), Pattern.Stone, Vector2.one);
        int graveyards = rng.Next(graveyardCount.x, Mathf.Max(graveyardCount.x, graveyardCount.y) + 1);
        for (int i = 0; i < graveyards; i++)
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var c = RandomPoint(edgeMargin + 4f);
            if (!DecorSpot(c, 4.5f, 6f) || GroundHeight(c.x, c.y) > MeshHeight(c.x, c.y) + 0.1f) continue;
            var yard = new GameObject("Кладбище").transform;
            yard.SetParent(group, false);
            yard.localPosition = new Vector3(c.x, 0f, c.y);
            yard.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            int rows = rng.Next(2, 4), cols = rng.Next(2, 4);
            for (int rr = 0; rr < rows; rr++)
            for (int cc = 0; cc < cols; cc++)
            {
                if (rng.NextDouble() < 0.15) continue; // пустое место в ряду
                var local = new Vector3((cc - (cols - 1) * 0.5f) * 1.8f + Range(-0.2f, 0.2f), 0f, (rr - (rows - 1) * 0.5f) * 2.2f);
                var onMap = root.InverseTransformPoint(yard.TransformPoint(local));
                float y = MeshHeight(onMap.x, onMap.z);
                float h = Range(0.7f, 1.1f);
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube); // надгробие с коллайдером
                stone.name = "Надгробие";
                stone.transform.SetParent(yard, false);
                stone.transform.localPosition = new Vector3(local.x, y + h * 0.4f, local.z);
                stone.transform.localRotation = Quaternion.Euler(Range(-8f, 8f), Range(-6f, 6f), Range(-6f, 6f));
                stone.transform.localScale = new Vector3(Range(0.55f, 0.75f), h, 0.16f);
                stone.GetComponent<Renderer>().sharedMaterial = stoneMat;
            }
            // Крест в стороне.
            var crossLocal = new Vector3(0f, 0f, -(rows * 1.1f + 1.2f));
            var crossOnMap = root.InverseTransformPoint(yard.TransformPoint(crossLocal));
            float cy = MeshHeight(crossOnMap.x, crossOnMap.z);
            DecorCube(yard, stoneMat, crossLocal + new Vector3(0f, cy + 0.9f, 0f), new Vector3(0.2f, 1.8f, 0.2f), Quaternion.identity, "Крест");
            DecorCube(yard, stoneMat, crossLocal + new Vector3(0f, cy + 1.35f, 0f), new Vector3(0.9f, 0.2f, 0.2f), Quaternion.identity, "Крест");
            occupied.Add(new Vector3(c.x, 4.5f, c.y));
            break;
        }

        rng = mainRng;
    }

    // ---------- Зомби: точки спавна ----------

    readonly List<ZombieSpawnPoint> zombieSpawns = new List<ZombieSpawnPoint>();
    /// Точки спавна зомби последней генерации.
    public IReadOnlyList<ZombieSpawnPoint> ZombieSpawns => zombieSpawns;

    /// Точки спавна зомби. Каждая стоит в основной проходимой области карты — той же, где появляется игрок,
    /// так что от неё можно дойти до игрока пешком. Сначала гнёзда у кладбищ, костей и руин, затем точки ближе к краям.
    /// Свой генератор случайных чисел — расстановка остальной карты не меняется.
    void PlaceZombieSpawns(Transform root)
    {
        zombieSpawns.Clear();
        var mainRng = rng;
        rng = new System.Random(seed ^ 0x2033B1E);
        int target = rng.Next(zombieSpawnCount.x, Mathf.Max(zombieSpawnCount.x, zombieSpawnCount.y) + 1);
        if (target <= 0) { rng = mainRng; return; }

        var group = new GameObject("Спавн зомби").transform;
        group.SetParent(root, false);
        var playerLocal = root.InverseTransformPoint(PlayerSpawnPoint);
        var player2 = new Vector2(playerLocal.x, playerLocal.z);

        var dirtMat = GetMaterial(new Color(0.24f, 0.17f, 0.11f), Pattern.Dirt, Vector2.one);
        var placed = new List<Vector2>();

        bool TryAdd(Vector2 p, bool nest)
        {
            if (Vector2.Distance(p, player2) < zombieMinDistanceFromPlayer) return false;
            foreach (var q in placed) if (Vector2.Distance(p, q) < zombieSpawnSpacing) return false;
            if (InWater(p, 1f) || !InWalkRegion(p) || !IsFree(p, 1f)) return false;
            var world = root.TransformPoint(new Vector3(p.x, GroundHeight(p.x, p.y), p.y));

            var go = new GameObject(nest ? "Гнездо зомби" : "Спавн зомби");
            go.transform.SetParent(group, false);
            go.transform.position = world;
            go.transform.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            var sp = go.AddComponent<ZombieSpawnPoint>();
            sp.isNest = nest;
            if (zombieSpawnMarkers)
            {
                // Разрытая земля: холмик и комья. Без коллайдера — не мешает ни игроку, ни зомби.
                float s = nest ? 1.4f : 1f;
                DecorCube(go.transform, dirtMat, new Vector3(0f, 0.04f, 0f), new Vector3(1.8f, 0.22f, 1.2f) * s, Quaternion.Euler(0f, 0f, Range(-4f, 4f)), "Холмик");
                int clods = rng.Next(3, 6);
                for (int i = 0; i < clods; i++)
                    DecorCube(go.transform, dirtMat, new Vector3(Range(-1.3f, 1.3f) * s, 0.08f, Range(-1f, 1f) * s), Vector3.one * Range(0.15f, 0.3f),
                        Quaternion.Euler(Range(0f, 90f), Range(0f, 90f), Range(0f, 90f)), "Ком");
            }
            zombieSpawns.Add(sp);
            placed.Add(p);
            return true;
        }

        // Гнёзда: кладбища, кучки костей, руины.
        var nests = new List<Vector2>();
        var camps = root.Find("Лагеря и ориентиры");
        if (camps != null) foreach (Transform t in camps) if (t.name == "Кладбище") nests.Add(new Vector2(t.localPosition.x, t.localPosition.z));
        var extras = root.Find("Дополнения");
        if (extras != null) foreach (Transform t in extras) if (t.name == "Кости") nests.Add(new Vector2(t.localPosition.x, t.localPosition.z));
        nests.AddRange(Positions(root, ruinCategory));
        int nestTarget = Mathf.RoundToInt(target * zombieNestShare);
        for (int i = 0; i < nests.Count && zombieSpawns.Count < nestTarget; i++)
        {
            var c = nests[rng.Next(nests.Count)];
            for (int attempt = 0; attempt < 6; attempt++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                if (TryAdd(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range(2f, 5f), true)) break;
            }
        }

        // Остальные: чаще у краёв карты, чтобы зомби приходили "из-за горизонта".
        float m = edgeMargin;
        for (int attempt = 0; attempt < target * 30 && zombieSpawns.Count < target; attempt++)
        {
            Vector2 p;
            if (rng.NextDouble() < 0.6)
            {
                float band = Range(m, m + 25f);
                float along = Range(-1f, 1f);
                int side = rng.Next(4);
                var half = mapSize * 0.5f;
                p = side == 0 ? new Vector2(along * (half.x - m), half.y - band) : side == 1 ? new Vector2(along * (half.x - m), -half.y + band)
                  : side == 2 ? new Vector2(half.x - band, along * (half.y - m)) : new Vector2(-half.x + band, along * (half.y - m));
            }
            else p = new Vector2(Range(MapMin.x + m, -MapMin.x - m), Range(MapMin.y + m, -MapMin.y - m));
            TryAdd(p, false);
        }

        if (zombieSpawns.Count < zombieSpawnCount.x)
            Debug.LogWarning($"[MapGenerator] Точки спавна зомби: поставлено {zombieSpawns.Count} из минимума {zombieSpawnCount.x}. Уменьшите zombieMinDistanceFromPlayer/zombieSpawnSpacing или увеличьте карту.", this);
        rng = mainRng;
    }

    static List<Vector2> Positions(Transform root, string category)
    {
        var list = new List<Vector2>();
        var g = root.Find(category);
        if (g != null) foreach (Transform t in g) list.Add(new Vector2(t.localPosition.x, t.localPosition.z));
        return list;
    }

    /// Тропинка: полоса утоптанной земли по слегка изогнутой кривой, повторяет рельеф.
    /// Там, где кривая проходит по плато, подъёму или воде, полоса прерывается.
    void BuildPath(Transform parent, Vector2 a, Vector2 b, Material mat, int number)
    {
        float dist = Vector2.Distance(a, b);
        if (dist < 4f) return;
        var dir = (b - a) / dist;
        var perp = new Vector2(-dir.y, dir.x);
        var ctrl = (a + b) * 0.5f + perp * (dist * Range(-0.25f, 0.25f));
        int n = Mathf.CeilToInt(dist / 0.6f);
        float noiseSeed = Range(0f, 100f);

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        var ok = new List<bool>();
        float along = 0f;
        Vector2 prev = a;
        var run = new List<(Vector2, Vector2)>();
        for (int i = 0; i <= n; i++)
        {
            float t = (float)i / n;
            var p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * ctrl + t * t * b;
            var tangent = (2 * (1 - t) * (ctrl - a) + 2 * t * (b - ctrl)).normalized;
            var side = new Vector2(-tangent.y, tangent.x) * (pathWidth * 0.5f * (0.8f + 0.4f * Mathf.PerlinNoise(t * 6f, noiseSeed)));
            along += Vector2.Distance(prev, p); prev = p;
            var l = p + side; var r = p - side;
            verts.Add(new Vector3(l.x, MeshHeight(l.x, l.y) + 0.06f, l.y));
            verts.Add(new Vector3(r.x, MeshHeight(r.x, r.y) + 0.06f, r.y));
            uvs.Add(new Vector2(0f, along / textureTileMeters));
            uvs.Add(new Vector2(pathWidth / textureTileMeters, along / textureTileMeters));
            bool onStructure = GroundHeight(p.x, p.y) > MeshHeight(p.x, p.y) + 0.1f || NearRamp(p, pathWidth * 0.5f) || InWater(p, 0f);
            ok.Add(!onStructure);
            if (onStructure) { if (run.Count > 1) pathRuns.Add(run); run = new List<(Vector2, Vector2)>(); }
            else run.Add((p, tangent));
            if (i > 0 && ok[i] && ok[i - 1])
            {
                int v0 = (i - 1) * 2, v1 = v0 + 1, v2 = v0 + 2, v3 = v0 + 3;
                // Обход по часовой стрелке сверху, чтобы лицевая сторона смотрела вверх.
                tris.Add(v0); tris.Add(v2); tris.Add(v1);
                tris.Add(v1); tris.Add(v2); tris.Add(v3);
            }
        }
        if (run.Count > 1) pathRuns.Add(run);
        if (tris.Count == 0) return;
        var mesh = new Mesh { name = $"Path_{number}" };
        generatedAssets.Add(mesh);
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var go = new GameObject($"Тропинка_{number:00}");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
    }

    // ---------- Проходимость ----------

    /// Разбивает сетку на связные области: соседи связаны, если перепад по силам пройти пешком
    /// в обе стороны (уклон <= walkableSlope). Оставляет самую большую область.
    void BuildWalkRegion()
    {
        int n = resolution + 1;
        var cell = CellSize;
        surface = new float[n, n];
        for (int x = 0; x < n; x++)
        for (int z = 0; z < n; z++)
            surface[x, z] = GroundHeight(MapMin.x + x * cell.x, MapMin.y + z * cell.y);

        float tan = Mathf.Tan(walkableSlope * Mathf.Deg2Rad);
        float limitX = tan * cell.x, limitZ = tan * cell.y;
        var comp = new int[n, n];
        var queue = new Queue<Vector2Int>();
        int best = 0, bestSize = 0, id = 0;

        for (int sx = 0; sx < n; sx++)
        for (int sz = 0; sz < n; sz++)
        {
            if (comp[sx, sz] != 0) continue;
            id++;
            int size = 0;
            comp[sx, sz] = id;
            queue.Enqueue(new Vector2Int(sx, sz));
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                size++;
                void Try(int x, int z, float limit)
                {
                    if (x < 0 || z < 0 || x >= n || z >= n || comp[x, z] != 0) return;
                    if (Mathf.Abs(surface[x, z] - surface[c.x, c.y]) > limit) return;
                    comp[x, z] = id;
                    queue.Enqueue(new Vector2Int(x, z));
                }
                Try(c.x + 1, c.y, limitX); Try(c.x - 1, c.y, limitX);
                Try(c.x, c.y + 1, limitZ); Try(c.x, c.y - 1, limitZ);
            }
            if (size > bestSize) { bestSize = size; best = id; }
        }

        walkRegion = new bool[n, n];
        for (int x = 0; x < n; x++)
        for (int z = 0; z < n; z++)
            walkRegion[x, z] = comp[x, z] == best;
        WalkableFraction = (float)bestSize / (n * n);
    }

    /// Запасной вариант спавна игрока: ближайшая к центру точка проходимой области.
    Vector2 FirstWalkablePoint()
    {
        var cell = CellSize;
        Vector2 best = Vector2.zero;
        float bestDist = float.MaxValue;
        for (int x = 0; x <= resolution; x++)
        for (int z = 0; z <= resolution; z++)
        {
            if (!walkRegion[x, z]) continue;
            var p = new Vector2(MapMin.x + x * cell.x, MapMin.y + z * cell.y);
            if (p.sqrMagnitude < bestDist) { bestDist = p.sqrMagnitude; best = p; }
        }
        return best;
    }

    bool InWalkRegion(Vector2 p)
    {
        var cell = CellSize;
        int x = Mathf.Clamp(Mathf.RoundToInt((p.x - MapMin.x) / cell.x), 0, resolution);
        int z = Mathf.Clamp(Mathf.RoundToInt((p.y - MapMin.y) / cell.y), 0, resolution);
        return walkRegion[x, z];
    }

    // ---------- Спавн объектов ----------

    /// Как IsFree, но не учитывает свободную зону объекта, стоящего в точке except (магазин, точка спавна).
    bool IsFreeExcept(Vector2 p, float radius, Vector2 except)
    {
        foreach (var o in occupied)
        {
            var oc = new Vector2(o.x, o.z);
            if ((oc - except).sqrMagnitude < 0.01f) continue;
            if (Vector2.Distance(p, oc) < radius + o.y) return false;
        }
        return true;
    }

    bool IsFree(Vector2 p, float radius)
    {
        foreach (var o in occupied)
            if (Vector2.Distance(p, new Vector2(o.x, o.z)) < radius + o.y) return false;
        return true;
    }

    /// Пол под объектом ровный в пределах наклона: объект радиусом footprint не свисает с края плато.
    bool FootprintFlat(Vector2 p, float footprint, float maxSlope)
    {
        float hc = GroundHeight(p.x, p.y);
        float allowed = Mathf.Tan(maxSlope * Mathf.Deg2Rad) * footprint + 0.3f;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            if (Mathf.Abs(GroundHeight(p.x + Mathf.Cos(a) * footprint, p.y + Mathf.Sin(a) * footprint) - hc) > allowed) return false;
        }
        return true;
    }

    /// Ищет случайную свободную точку с подходящим наклоном. footprint — радиус самого объекта. null — если места нет.
    Vector2? FindSpot(float radius, float maxSlope, bool reachable, float footprint)
    {
        float m = Mathf.Min(edgeMargin, mapSize.x * 0.45f, mapSize.y * 0.45f);
        for (int i = 0; i < PlaceAttempts; i++)
        {
            var p = new Vector2(Range(MapMin.x + m, -MapMin.x - m), Range(MapMin.y + m, -MapMin.y - m));
            if (reachable && !InWalkRegion(p)) continue;
            if (NearRamp(p, radius + 1.5f)) continue; // не загораживать пандусы
            if (InWater(p, radius)) continue;
            if (SlopeAngle(p.x, p.y) <= maxSlope && IsFree(p, radius) && FootprintFlat(p, footprint, maxSlope)) return p;
        }
        return null;
    }

    /// Ставит объект так, чтобы низ его bounds касался земли.
    void PlaceOnGround(Transform t, Vector2 p)
    {
        float ground = GroundHeight(p.x, p.y);
        t.localPosition = new Vector3(p.x, ground, p.y);
        // Только видимые меши: у частиц, линий и выключенных эффектов габариты в (0,0,0) мира и сбили бы высоту.
        var renderers = new List<Renderer>();
        foreach (var r in t.GetComponentsInChildren<Renderer>())
            if (r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer)) renderers.Add(r);
        if (renderers.Count == 0) return;
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        float groundWorld = transform.TransformPoint(t.localPosition).y;
        t.position += Vector3.up * (groundWorld - b.min.y);
    }

    void SpawnPlayer(Transform root)
    {
        var p = FindSpot(playerClearRadius, 25f, true, 1f) ?? FirstWalkablePoint();
        occupied.Add(new Vector3(p.x, playerClearRadius, p.y));

        Transform t = player;
        if (t == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "PlayerSpawn";
            go.GetComponent<Renderer>().sharedMaterial = GetMaterial(Color.white);
            t = go.transform;
            t.SetParent(root, false);
        }

        var cc = t.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // иначе CharacterController перезапишет позицию
        PlaceOnGround(t, p);
        if (cc != null) cc.enabled = true;
        var rb = t.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;

        PlayerSpawnPoint = t.position;
    }

    void SpawnCategoryObjects(SpawnCategory c, Transform root)
    {
        var group = new GameObject(c.name).transform;
        group.SetParent(root, false);

        // Радиус объекта по горизонтали (полудиагональ — с запасом на случайный поворот).
        // shortcut: для префабов размер неизвестен до создания, берём clearRadius; при необходимости — мерить bounds префаба.
        float footprint = c.prefab != null
            ? c.clearRadius
            : new Vector2(c.size.x, c.size.z).magnitude * 0.5f * c.scaleRange.y;
        // Составные фигуры шире своего size: обломок у глыбы, пень у бревна, проломы и упавший блок у руин.
        if (c.prefab == null)
            switch (c.look)
            {
                case Look.Boulder: footprint = Mathf.Max(c.size.x, c.size.z) * 1.05f * c.scaleRange.y; break;
                case Look.Log: footprint = (c.size.x * 0.5f + c.size.y * 1.8f) * c.scaleRange.y; break;
                case Look.Ruin: footprint = (c.size.x * 0.5f + 1.5f) * c.scaleRange.y; break;
            }

        int target = rng.Next(c.minCount, c.maxCount + 1);
        int placed = 0;
        for (int i = 0; i < target; i++)
        {
            if (i >= c.minCount && rng.NextDouble() > c.spawnChance) continue;
            var spot = FindSpot(c.clearRadius, c.maxSlope, c.mustBeReachable, footprint);
            if (spot == null) continue;
            var p = spot.Value;
            occupied.Add(new Vector3(p.x, c.clearRadius, p.y));

            float s = Range(c.scaleRange.x, c.scaleRange.y);
            GameObject go;
            if (c.prefab != null)
            {
                go = Instantiate(c.prefab);
                go.transform.localScale *= s;
            }
            else go = BuildLook(c, c.size * s);
            go.name = $"{c.name}_{++placed:00}";
            var t = go.transform;
            t.SetParent(group, false);
            if (c.randomYRotation) t.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            PlaceOnGround(t, p);
            if (c.prefab == null) SnapPendingParts();
        }

        if (placed < c.minCount)
            Debug.LogWarning($"[MapGenerator] {c.name}: поставлено {placed} из минимума {c.minCount} — не хватило места. Уменьшите clearRadius/количество или увеличьте карту.", this);
    }

    readonly List<(Transform part, float sink)> pendingGround = new List<(Transform, float)>();

    /// Сажает отмеченные детали последнего построенного объекта на землю по отдельности:
    /// низ детали — на самую низкую точку земли под ней, затем утопить на долю sink её высоты.
    void SnapPendingParts()
    {
        foreach (var (part, sink) in pendingGround)
        {
            if (part == null) continue;
            var r = part.GetComponent<Renderer>();
            if (r == null) continue;
            var b = r.bounds;
            float ground = float.MaxValue;
            foreach (var corner in new[] { new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.min.x, 0f, b.max.z), new Vector3(b.max.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.max.z), b.center })
            {
                var lp = transform.InverseTransformPoint(corner);
                ground = Mathf.Min(ground, GroundHeight(lp.x, lp.z));
            }
            var c = transform.InverseTransformPoint(b.center);
            float groundWorld = transform.TransformPoint(new Vector3(c.x, ground, c.z)).y;
            part.position += Vector3.up * (groundWorld - sink * b.size.y - b.min.y);
        }
        pendingGround.Clear();
    }

    /// Объект категории из примитивов. size — итоговый размер (уже с учётом случайного масштаба).
    GameObject BuildLook(SpawnCategory c, Vector3 size)
    {
        float tile = textureTileMeters;
        if (c.look == Look.Primitive)
        {
            var prim = GameObject.CreatePrimitive(c.primitive);
            prim.GetComponent<Renderer>().sharedMaterial = GetMaterial(c.color, Pattern.None, default, c.glow);
            prim.transform.localScale = size;
            return prim;
        }

        var root = new GameObject();
        pendingGround.Clear();
        // ground >= 0: деталь после установки объекта отдельно сажается на землю и утапливается на эту долю своей высоты
        // (обломки руин, глыбы, брёвна на склоне иначе висят в воздухе).
        GameObject Part(PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, Quaternion rot, bool collider = true, float ground = -1f)
        {
            var part = GameObject.CreatePrimitive(type);
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = pos;
            part.transform.localRotation = rot;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Kill(part.GetComponent<Collider>());
            if (ground >= 0f) pendingGround.Add((part.transform, ground));
            return part;
        }
        Quaternion RandomRot() => Quaternion.Euler(Range(0f, 360f), Range(0f, 360f), Range(0f, 360f));

        switch (c.look)
        {
            case Look.Tree:
            {
                // Ствол-цилиндр и крона из нескольких повёрнутых кубов — "кубические" деревья как в Megabonk.
                float h = size.y, w = size.x;
                Part(PrimitiveType.Cylinder, GetMaterial(trunkColor, Pattern.Bark, new Vector2(1f, h * 0.6f / tile)),
                    new Vector3(0f, h * 0.3f, 0f), new Vector3(w * 0.3f, h * 0.3f, w * 0.3f), Quaternion.identity);
                int blobs = rng.Next(3, 6);
                for (int i = 0; i < blobs; i++)
                {
                    float shade = new[] { 0.85f, 1f, 1.12f }[rng.Next(3)];
                    var leaf = GetMaterial(c.color * shade, Pattern.Leaves, Vector2.one);
                    float b = h * Range(0.28f, 0.42f);
                    Part(PrimitiveType.Cube, leaf,
                        new Vector3(Range(-0.17f, 0.17f) * h, h * Range(0.62f, 0.88f), Range(-0.17f, 0.17f) * h),
                        Vector3.one * b, RandomRot(), collider: false);
                }
                break;
            }
            case Look.Boulder:
            {
                var mat = GetMaterial(c.color, Pattern.Stone, Vector2.one);
                Part(PrimitiveType.Cube, mat, Vector3.zero, size, Quaternion.Euler(Range(-20f, 20f), 0f, Range(-20f, 20f)), ground: 0.25f);
                // Иногда рядом лежит обломок поменьше.
                if (rng.NextDouble() < 0.5)
                    Part(PrimitiveType.Cube, mat, new Vector3(size.x * 0.7f, -size.y * 0.15f, size.z * Range(-0.4f, 0.4f)), size * Range(0.35f, 0.55f), RandomRot(), ground: 0.3f);
                break;
            }
            case Look.Ruin:
            {
                // Обломки стены разной высоты с проломами и упавший блок рядом.
                int segments = rng.Next(2, 5);
                float x = -size.x * 0.5f;
                for (int i = 0; i < segments; i++)
                {
                    float len = size.x / segments * Range(0.6f, 1.2f);
                    float hgt = size.y * Range(0.35f, 1f);
                    var mat = GetMaterial(c.color, Pattern.Bricks, new Vector2(len, hgt) / tile * 2f);
                    Part(PrimitiveType.Cube, mat, new Vector3(x + len * 0.5f, hgt * 0.5f, Range(-0.1f, 0.1f)), new Vector3(len, hgt, size.z), Quaternion.identity, ground: 0.08f);
                    x += len + Range(0f, 0.8f);
                }
                if (rng.NextDouble() < 0.6)
                    Part(PrimitiveType.Cube, GetMaterial(c.color, Pattern.Bricks, Vector2.one),
                        new Vector3(Range(-0.4f, 0.4f) * size.x, size.z * 0.4f, size.z * Range(1.2f, 2f)),
                        new Vector3(size.z * 1.6f, size.z * 0.8f, size.z), Quaternion.Euler(0f, Range(0f, 360f), Range(-10f, 10f)), ground: 0.15f);
                break;
            }
            case Look.Bush:
            {
                // Низкий куст из 2–4 повёрнутых кубов листвы; сквозь него можно пройти.
                int blobs = rng.Next(2, 5);
                for (int i = 0; i < blobs; i++)
                {
                    float shade = new[] { 0.8f, 0.95f, 1.1f }[rng.Next(3)];
                    float b = size.y * Range(0.6f, 0.95f);
                    Part(PrimitiveType.Cube, GetMaterial(c.color * shade, Pattern.Leaves, Vector2.one),
                        new Vector3(Range(-0.3f, 0.3f) * size.x, b * 0.45f, Range(-0.3f, 0.3f) * size.z),
                        Vector3.one * b, RandomRot(), collider: false);
                }
                break;
            }
            case Look.Log:
            {
                // Поваленное бревно: цилиндр на боку (длина — size.x, толщина — size.y) и иногда пень рядом.
                var bark = GetMaterial(c.color, Pattern.Bark, new Vector2(1f, size.x / tile));
                Part(PrimitiveType.Cylinder, bark, new Vector3(0f, size.y * 0.5f, 0f),
                    new Vector3(size.y, size.x * 0.5f, size.y), Quaternion.Euler(0f, 0f, 90f), ground: 0.12f);
                if (rng.NextDouble() < 0.5)
                    Part(PrimitiveType.Cylinder, bark, new Vector3(size.x * 0.5f + size.y, size.y * 0.4f, Range(-0.5f, 0.5f)),
                        new Vector3(size.y * 1.3f, size.y * 0.4f, size.y * 1.3f), Quaternion.identity, ground: 0.1f);
                break;
            }
            case Look.Pillar:
            {
                // Каменный столб: 2–3 блока, каждый уже и чуть повёрнут — как выветренные скалы.
                int parts = rng.Next(2, 4);
                float y = 0f, w = size.x;
                for (int i = 0; i < parts; i++)
                {
                    float h = size.y / parts * Range(0.8f, 1.2f);
                    var mat = GetMaterial(c.color * Range(0.9f, 1.05f), Pattern.Stone, new Vector2(w, h) / tile);
                    Part(PrimitiveType.Cube, mat, new Vector3(Range(-0.15f, 0.15f) * w, y + h * 0.5f, Range(-0.15f, 0.15f) * w),
                        new Vector3(w, h, w * Range(0.8f, 1f)), Quaternion.Euler(Range(-4f, 4f), Range(0f, 90f), Range(-4f, 4f)));
                    y += h * 0.95f;
                    w *= Range(0.65f, 0.85f);
                }
                break;
            }
            case Look.Mushroom:
            {
                // 1–3 гриба: светлая ножка и приплюснутая шляпка цвета категории.
                var stem = GetMaterial(new Color(0.9f, 0.87f, 0.78f));
                var capMat = GetMaterial(c.color, Pattern.Leaves, Vector2.one);
                int count = rng.Next(1, 4);
                for (int i = 0; i < count; i++)
                {
                    float k = i == 0 ? 1f : Range(0.45f, 0.75f);
                    var off = i == 0 ? Vector3.zero : new Vector3(Range(-0.6f, 0.6f), 0f, Range(-0.6f, 0.6f)) * size.x;
                    float h = size.y * k, w = size.x * k;
                    Part(PrimitiveType.Cylinder, stem, off + new Vector3(0f, h * 0.35f, 0f), new Vector3(w * 0.3f, h * 0.35f, w * 0.3f), Quaternion.identity, collider: false);
                    Part(PrimitiveType.Sphere, capMat, off + new Vector3(0f, h * 0.75f, 0f), new Vector3(w, h * 0.45f, w),
                        Quaternion.Euler(Range(-10f, 10f), 0f, Range(-10f, 10f)), collider: false);
                }
                break;
            }
            case Look.Crate:
            {
                // Деревянный ящик, иногда второй сверху, повёрнутый.
                var wood = GetMaterial(c.color, Pattern.Bark, Vector2.one);
                Part(PrimitiveType.Cube, wood, new Vector3(0f, size.y * 0.5f, 0f), size, Quaternion.identity);
                if (rng.NextDouble() < 0.4)
                    Part(PrimitiveType.Cube, GetMaterial(c.color * 0.9f, Pattern.Bark, Vector2.one),
                        new Vector3(0f, size.y * 1.35f, 0f), size * 0.7f, Quaternion.Euler(0f, Range(10f, 40f), 0f));
                break;
            }
            case Look.Barrel:
            {
                // Бочка с двумя тёмными обручами.
                Part(PrimitiveType.Cylinder, GetMaterial(c.color, Pattern.Bark, Vector2.one),
                    new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x, size.y * 0.5f, size.z), Quaternion.identity);
                var hoop = GetMaterial(new Color(0.22f, 0.2f, 0.2f));
                foreach (float y in new[] { 0.22f, 0.78f })
                    Part(PrimitiveType.Cylinder, hoop, new Vector3(0f, size.y * y, 0f), new Vector3(size.x * 1.05f, 0.04f, size.z * 1.05f), Quaternion.identity, collider: false);
                break;
            }
            case Look.Lantern:
            {
                // Фонарь: тёмный столб, светящийся плафон (bloom подхватывает), козырёк.
                var post = GetMaterial(new Color(0.3f, 0.22f, 0.15f), Pattern.Bark, Vector2.one);
                Part(PrimitiveType.Cube, post, new Vector3(0f, size.y * 0.45f, 0f), new Vector3(size.x * 0.35f, size.y * 0.9f, size.z * 0.35f), Quaternion.identity);
                Part(PrimitiveType.Cube, GetMaterial(c.color, Pattern.None, default, 1.3f), new Vector3(0f, size.y * 0.93f, 0f),
                    new Vector3(size.x, size.x * 1.1f, size.z), Quaternion.Euler(0f, 45f, 0f), collider: false);
                Part(PrimitiveType.Cube, post, new Vector3(0f, size.y * 0.93f + size.x * 0.65f, 0f), new Vector3(size.x * 1.4f, 0.08f, size.z * 1.4f),
                    Quaternion.Euler(0f, 45f, 0f), collider: false);
                break;
            }
            case Look.Campfire:
            {
                // Кольцо камней, сложенные шалашом поленья и светящиеся угли.
                var stone = GetMaterial(new Color(0.5f, 0.49f, 0.47f), Pattern.Stone, Vector2.one);
                int stones = rng.Next(7, 10);
                for (int i = 0; i < stones; i++)
                {
                    float a = i * Mathf.PI * 2f / stones;
                    Part(PrimitiveType.Cube, stone, new Vector3(Mathf.Cos(a), 0.12f, Mathf.Sin(a)) * (size.x * 0.5f),
                        Vector3.one * Range(0.25f, 0.38f), RandomRot(), collider: false);
                }
                var wood = GetMaterial(trunkColor, Pattern.Bark, Vector2.one);
                for (int i = 0; i < 3; i++)
                    Part(PrimitiveType.Cylinder, wood, new Vector3(0f, size.y * 0.35f, 0f), new Vector3(0.12f, size.y * 0.45f, 0.12f),
                        Quaternion.Euler(0f, i * 120f, 0f) * Quaternion.Euler(35f, 0f, 0f), collider: false);
                Part(PrimitiveType.Cube, GetMaterial(c.color, Pattern.None, default, 2f), new Vector3(0f, 0.12f, 0f),
                    new Vector3(size.x * 0.4f, 0.18f, size.x * 0.4f), Quaternion.Euler(0f, 45f, 0f), collider: false);
                break;
            }
            case Look.Arch:
            {
                // Две колонны и перекладина (size.x — пролёт, size.y — высота, size.z — толщина). Иногда перекладина упала.
                var stone = GetMaterial(c.color, Pattern.Bricks, new Vector2(1f, size.y / tile));
                float half = size.x * 0.5f;
                foreach (float sx in new[] { -half, half })
                {
                    float h = size.y * (rng.NextDouble() < 0.3 ? Range(0.5f, 0.8f) : 1f);
                    Part(PrimitiveType.Cube, stone, new Vector3(sx, h * 0.5f, 0f), new Vector3(size.z, h, size.z), Quaternion.Euler(0f, Range(-3f, 3f), 0f));
                }
                if (rng.NextDouble() < 0.7)
                    Part(PrimitiveType.Cube, GetMaterial(c.color, Pattern.Bricks, new Vector2(size.x / tile, 1f)),
                        new Vector3(0f, size.y + size.z * 0.4f, 0f), new Vector3(size.x + size.z * 1.4f, size.z * 0.8f, size.z * 1.1f), Quaternion.identity);
                else
                    Part(PrimitiveType.Cube, GetMaterial(c.color, Pattern.Bricks, new Vector2(size.x / tile, 1f)),
                        new Vector3(Range(-1f, 1f), size.z * 0.4f, size.z * 1.8f), new Vector3(size.x * 0.8f, size.z * 0.8f, size.z * 1.1f),
                        Quaternion.Euler(0f, Range(-20f, 20f), Range(-6f, 6f)));
                break;
            }
            case Look.Tent:
            {
                // Палатка-домик: два ската ткани, тёмный вход спереди, задняя стенка. size: ширина, высота, длина.
                var cloth = GetMaterial(c.color, Pattern.Leaves, Vector2.one);
                float halfW = size.x * 0.5f, h = size.y, len = size.z;
                float slope = Mathf.Sqrt(halfW * halfW + h * h);
                float ang = Mathf.Atan2(h, halfW) * Mathf.Rad2Deg;
                foreach (float s in new[] { -1f, 1f })
                    Part(PrimitiveType.Cube, cloth, new Vector3(s * halfW * 0.5f, h * 0.5f, 0f), new Vector3(slope, 0.06f, len),
                        Quaternion.Euler(0f, 0f, -s * ang));
                Part(PrimitiveType.Cube, GetMaterial(c.color * 0.75f), new Vector3(0f, h * 0.45f, -len * 0.5f), new Vector3(size.x * 0.55f, h * 0.85f, 0.04f),
                    Quaternion.identity, collider: false);
                Part(PrimitiveType.Cube, GetMaterial(new Color(0.12f, 0.1f, 0.08f)), new Vector3(0f, h * 0.3f, len * 0.5f), new Vector3(size.x * 0.35f, h * 0.55f, 0.04f),
                    Quaternion.identity, collider: false);
                break;
            }
            case Look.Cart:
            {
                // Телега: кузов, борта, колёса на боку, оглобли. Иногда одного колеса нет и телега завалилась.
                var wood = GetMaterial(c.color, Pattern.Bark, Vector2.one);
                var wheelMat = GetMaterial(c.color * 0.6f, Pattern.Bark, Vector2.one);
                bool broken = rng.NextDouble() < 0.35;
                float tilt = broken ? Range(8f, 14f) : 0f;
                var body = Quaternion.Euler(0f, 0f, tilt);
                float bodyY = size.y * 0.75f;
                Part(PrimitiveType.Cube, wood, body * new Vector3(0f, bodyY, 0f), new Vector3(size.x, 0.12f, size.z), body);
                foreach (float s in new[] { -1f, 1f })
                    Part(PrimitiveType.Cube, wood, body * new Vector3(s * size.x * 0.5f, bodyY + 0.25f, 0f), new Vector3(0.08f, 0.45f, size.z), body);
                Part(PrimitiveType.Cube, wood, body * new Vector3(0f, bodyY + 0.25f, -size.z * 0.5f), new Vector3(size.x, 0.45f, 0.08f), body);
                float wr = size.y * 0.5f;
                foreach (float s in new[] { -1f, 1f })
                {
                    if (broken && s > 0f) // колесо отвалилось и лежит рядом
                    {
                        Part(PrimitiveType.Cylinder, wheelMat, new Vector3(size.x * 0.5f + wr * 1.4f, 0.06f, Range(-0.5f, 0.5f)),
                            new Vector3(wr * 2f, 0.06f, wr * 2f), Quaternion.identity, collider: false);
                        continue;
                    }
                    Part(PrimitiveType.Cylinder, wheelMat, new Vector3(s * (size.x * 0.5f + 0.08f), wr, 0f), new Vector3(wr * 2f, 0.06f, wr * 2f),
                        Quaternion.Euler(0f, 0f, 90f), collider: false);
                }
                foreach (float s in new[] { -0.35f, 0.35f })
                    Part(PrimitiveType.Cube, wood, new Vector3(s * size.x, bodyY - 0.15f, size.z * 0.5f + 0.8f), new Vector3(0.08f, 0.08f, 1.6f),
                        Quaternion.Euler(-12f, 0f, 0f), collider: false);
                break;
            }
            case Look.Well:
            {
                // Колодец: каменное кольцо, тёмная вода, два столба, перекладина и двускатная крыша.
                var stone = GetMaterial(c.color, Pattern.Stone, Vector2.one);
                float r = size.x * 0.5f;
                int blocksN = 10;
                for (int i = 0; i < blocksN; i++)
                {
                    float a = i * Mathf.PI * 2f / blocksN;
                    Part(PrimitiveType.Cube, stone, new Vector3(Mathf.Cos(a) * r, 0.4f, Mathf.Sin(a) * r), new Vector3(0.5f, 0.8f, 0.7f),
                        Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
                }
                Part(PrimitiveType.Cylinder, GetMaterial(new Color(0.08f, 0.14f, 0.18f)), new Vector3(0f, 0.55f, 0f), new Vector3(r * 1.7f, 0.02f, r * 1.7f),
                    Quaternion.identity, collider: false);
                var wood = GetMaterial(woodColor * 0.85f, Pattern.Bark, Vector2.one);
                foreach (float s in new[] { -1f, 1f })
                    Part(PrimitiveType.Cube, wood, new Vector3(s * r, size.y * 0.5f, 0f), new Vector3(0.14f, size.y, 0.14f), Quaternion.identity);
                Part(PrimitiveType.Cylinder, wood, new Vector3(0f, size.y * 0.75f, 0f), new Vector3(0.12f, r, 0.12f), Quaternion.Euler(0f, 0f, 90f), collider: false);
                var roof = GetMaterial(new Color(0.5f, 0.3f, 0.2f), Pattern.Bark, Vector2.one);
                foreach (float s in new[] { -1f, 1f })
                    Part(PrimitiveType.Cube, roof, new Vector3(0f, size.y + 0.2f, s * 0.45f), new Vector3(r * 2.6f, 0.06f, 1.1f),
                        Quaternion.Euler(s * 35f, 0f, 0f), collider: false);
                break;
            }
            case Look.Tuft:
            {
                // Пучок травинок, иногда с цветком. Без коллайдеров — это только декор.
                int blades = rng.Next(4, 8);
                for (int i = 0; i < blades; i++)
                {
                    float h = size.y * Range(0.9f, 1.7f);
                    float shade = new[] { 0.85f, 1f, 1.15f }[rng.Next(3)];
                    Part(PrimitiveType.Cube, GetMaterial(c.color * shade),
                        new Vector3(Range(-0.35f, 0.35f) * size.x, h * 0.5f, Range(-0.35f, 0.35f) * size.z),
                        new Vector3(0.14f, h, 0.14f), Quaternion.Euler(Range(-18f, 18f), Range(0f, 90f), Range(-18f, 18f)), collider: false);
                }
                if (rng.NextDouble() < 0.3)
                {
                    var flower = new[] { new Color(1f, 0.95f, 0.85f), new Color(1f, 0.85f, 0.2f), new Color(0.95f, 0.45f, 0.7f), new Color(0.6f, 0.5f, 1f) }[rng.Next(4)];
                    Part(PrimitiveType.Cube, GetMaterial(flower), new Vector3(0f, size.y * 1.5f, 0f), Vector3.one * 0.22f,
                        Quaternion.Euler(0f, 45f, 0f), collider: false);
                }
                break;
            }
        }
        return root;
    }

    static void Kill(UnityEngine.Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    // ---------- Материалы и текстуры ----------

    enum Pattern { None, Grass, Dirt, Stone, Bricks, Leaves, Bark }

    /// Материал цвета color с узором pattern; tiling — сколько раз узор повторяется на грани/меше.
    Material GetMaterial(Color color, Pattern pattern = Pattern.None, Vector2 tiling = default, float glow = 0f)
    {
        if (!pixelTextures) pattern = Pattern.None;
        if (tiling == default) tiling = Vector2.one;
        // Округляем тайлинг, чтобы похожие по размеру объекты делили один материал.
        tiling = new Vector2(Mathf.Max(1f, Mathf.Round(tiling.x)), Mathf.Max(1f, Mathf.Round(tiling.y)));
        string key = $"{ColorUtility.ToHtmlStringRGB(color)}_{pattern}_{tiling.x}x{tiling.y}_{glow:F1}";
        if (materials.TryGetValue(key, out var m)) return m;
        if (baseMaterial == null)
        {
            // Берём стандартный материал текущего рендер-пайплайна (URP Lit / Standard) у временного примитива.
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMaterial = tmp.GetComponent<Renderer>().sharedMaterial;
            if (Application.isPlaying) Destroy(tmp); else DestroyImmediate(tmp);
        }
        m = new Material(baseMaterial) { color = color, name = "Gen_" + key };
        if (pattern != Pattern.None)
        {
            m.mainTexture = GetPatternTexture(pattern);
            m.mainTextureScale = tiling;
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f); // матовые поверхности, без пластикового блеска
        if (glow > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * glow);
        }
        materials[key] = m;
        generatedAssets.Add(m);
        return m;
    }

    static readonly Dictionary<Pattern, Texture2D> patternCache = new Dictionary<Pattern, Texture2D>();

    /// Серая пиксельная текстура-узор 32x32 (цвет задаёт материал). Point-фильтр — "пиксельный" вид, как в Megabonk.
    static Texture2D GetPatternTexture(Pattern p)
    {
        if (patternCache.TryGetValue(p, out var tex) && tex != null) return tex;
        const int n = 32;
        tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, name = "Pattern_" + p };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float r = Hash(x, y, (int)p);          // шум на пиксель
            float r2 = Hash(x / 4, y / 4, (int)p + 7); // крупные пятна 4x4
            float v;
            switch (p)
            {
                case Pattern.Grass:  v = 0.84f + r * 0.14f + (r2 - 0.5f) * 0.2f - (r > 0.92f ? 0.18f : 0f) + (r < 0.04f ? 0.15f : 0f); break;
                case Pattern.Dirt:   v = 0.82f + r * 0.12f + Mathf.Sin(y * 0.9f + r2 * 2f) * 0.06f - ((y % 8 == 0) ? 0.12f : 0f); break;
                case Pattern.Stone:  v = 0.8f + r2 * 0.18f + r * 0.06f - ((x % 16 == 0 || y % 16 == 0) ? 0.12f : 0f); break;
                case Pattern.Bricks:
                {
                    int row = y / 8;
                    bool mortar = y % 8 == 0 || (x + (row % 2) * 8) % 16 == 0;
                    v = mortar ? 0.6f : 0.85f + r * 0.1f + (Hash((x + (row % 2) * 8) / 16, row, 3) - 0.5f) * 0.12f;
                    break;
                }
                case Pattern.Leaves: v = 0.75f + r2 * 0.25f + r * 0.08f - (r < 0.08f ? 0.2f : 0f); break;
                case Pattern.Bark:   v = 0.8f + Hash(x / 2, 0, 11) * 0.2f + r * 0.05f - ((x % 6 == 0) ? 0.15f : 0f); break;
                default: v = 1f; break;
            }
            v = Mathf.Clamp01(v);
            px[y * n + x] = new Color(v, v, v, 1f);
        }
        tex.SetPixels(px);
        tex.Apply();
        patternCache[p] = tex;
        return tex;
    }

    static float Hash(int x, int y, int s)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + s * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }
}
