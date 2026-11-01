using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GemporiumGrabItems
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class GrabItemsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "hovyf.gemporium.grab-items";
        public const string PluginName = "Gemporium Grab Items";
        public const string PluginVersion = "0.1.3";

        private const int Columns = 5;
        private const int Rows = 4;
        private const int PageSize = Columns * Rows;

        private enum ItemKind { BrokenGem, IntactGem, Artifact }

        private sealed class Entry
        {
            public ItemKind Kind;
            public string Name;
            public string SearchText;
            public Sprite Icon;
            public SO_GemProperties Gem;
            public SO_ArtifactProperties Artifact;
            public Color NameColor;
        }

        private static readonly Dictionary<string, string> ArtifactNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "RollerSkateArtifact", "Transportation Device" },
            { "SolarCellArtifact", "Battery" },
            { "StylusArtifact", "Packaged Tools" },
            { "TamagotchiArtifact", "Egg Device" },
            { "ThimbleArtifact", "Thimble" },
            { "TryceratopsArtifact", "Triceratops Skull" },
            { "VivosaurArtifact", "Strange Fossil" },
            { "BeanieBabyArtifact", "Plush Animal" },
            { "CicadaArtifact", "Bug" },
            { "FernFossilArtifact", "Fossilized Fern" },
            { "FlipPhoneArtifact", "Flipping Device" },
            { "IdolArtifact", "Little Guy" },
            { "RingTossArtifact", "Miniature Aquarium" },
            { "TaiyakiArtifact", "Taiyaki" },
            { "VinylArtifact", "Grooved Disk" },
            { "ArrowheadArtifact", "Arrowhead" },
            { "BearKnuckleArtifact", "Bestest Bear" },
            { "BreadArtifact", "World's Oldest Bread" },
            { "CheetoArtifact", "Cheeto's Collar" },
            { "GameboyArtifact", "Micro-Tech" },
            { "LeafFossilArtifact", "Fossilized Leaf" },
            { "LeekArtifact", "Fossilized Leek" },
            { "MeteorArtifact", "Fallen Star" },
            { "ShellArtifact", "Nautilus Shell" },
            { "SwordArtifact", "Awesome Freaking Dagger" },
            { "ACTArtifact", "Fork" },
            { "EllieArtifact", "Cozy Journal" },
            { "GlintseekerArtifact", "Sparky Stone" },
            { "HasteArtifact", "Speedy Hat" },
            { "ImCadeArtifact", "Broken Microphone" },
            { "MCBTYCArtifact", "Playing Card" },
            { "MailTimeArtifact", "Mushroom Cap" },
            { "NNFArtifact", "Crazy Straw+" },
            { "NikkyArtifact", "Nikky's Cozy Keychain" },
            { "ShroomArtifact", "Dormant Grunt" },
            { "SleddingTrophyArtifact", "1nd Place Trophy" }
        };

        private readonly List<Entry> _entries = new List<Entry>();
        private sealed class HitTarget
        {
            public Rect Bounds;
            public Action<int> Click;
        }
        private readonly List<HitTarget> _hitTargets = new List<HitTarget>();
        private Keyboard _textKeyboard;
        private bool _searchFocused;
        private Vector2 _pointer;
        private static GrabItemsPlugin _instance;
        private Harmony _harmony;
        private Mouse _hardwareMouse;
        private Behaviour _gameCursorImage;
        private bool _gameCursorImageEnabled;

        private void Awake()
        {
            _instance = this;
            _harmony = new Harmony(PluginGuid);
            try
            {
                var prefix = new HarmonyMethod(typeof(GrabItemsPlugin), nameof(AllowGameCursor));
                foreach (string method in new[] { "UpdateMouseState", "Update", "SetCursorVisibility" })
                {
                    var target = AccessTools.Method(typeof(VirtualCursor), method);
                    if (target == null) throw new MissingMethodException("VirtualCursor", method);
                    _harmony.Patch(target, prefix: prefix);
                }
                Logger.LogInfo(PluginVersion + ": virtual cursor recentering guard installed; polished gems, base-game items only.");
            }
            catch (Exception ex)
            {
                _harmony.UnpatchSelf();
                Logger.LogError(ex);
                enabled = false;
            }
        }

        private static bool AllowGameCursor()
        {
            return _instance == null || !_instance._open;
        }

        private void CaptureGameCursor()
        {
            VirtualCursor cursor = CursorManager.Instance != null ? CursorManager.Instance.VirtualCursor : null;
            _hardwareMouse = cursor != null ? AccessTools.Field(typeof(VirtualCursor), "m_systemMouse").GetValue(cursor) as Mouse : null;
            if (_hardwareMouse == null || !_hardwareMouse.added)
                _hardwareMouse = InputSystem.devices.OfType<Mouse>().FirstOrDefault(mouse => mouse.native && mouse.added);
            _gameCursorImage = cursor != null ? AccessTools.Field(typeof(VirtualCursor), "m_cursorImage").GetValue(cursor) as Behaviour : null;
            if (_gameCursorImage != null)
            {
                _gameCursorImageEnabled = _gameCursorImage.enabled;
                _gameCursorImage.enabled = false;
            }
        }
        private bool _open;
        private bool _actionsWereEnabled;
        private bool _cursorWasVisible;
        private CursorLockMode _cursorLockMode;
        private ItemKind _category = ItemKind.BrokenGem;
        private string _search = string.Empty;
        private string _status = "F8 关闭 / Close";
        private int _page;
        private GUIStyle _windowStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _selectedButtonStyle;
        private GUIStyle _nameStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _statusStyle;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                SetOpen(!_open);
                return;
            }
            if (!_open) return;
            if (_textKeyboard != Keyboard.current)
            {
                DetachTextInput();
                _textKeyboard = Keyboard.current;
                if (_textKeyboard != null) _textKeyboard.onTextInput += OnTextInput;
            }
            if (_searchFocused && Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame && _search.Length > 0)
            {
                _search = _search.Substring(0, _search.Length - 1);
                _page = 0;
            }
            if (_hardwareMouse == null || !_hardwareMouse.added) return;
            Vector2 position = _hardwareMouse.position.ReadValue();
            _pointer = new Vector2(position.x, Screen.height - position.y);
            int button = _hardwareMouse.leftButton.wasPressedThisFrame ? 0 : _hardwareMouse.rightButton.wasPressedThisFrame ? 1 : -1;
            if (button < 0) return;
            _searchFocused = false;
            foreach (HitTarget target in _hitTargets.ToArray())
            {
                if (!target.Bounds.Contains(_pointer)) continue;
                target.Click(button);
                // The callback can change category/search/page; discard old hit regions.
                _hitTargets.Clear();
                break;
            }
        }

        private void LateUpdate()
        {
            if (!_open) return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnTextInput(char value)
        {
            if (!_open || !_searchFocused || char.IsControl(value) || _search.Length >= 100) return;
            _search += value;
            _page = 0;
        }

        private void DetachTextInput()
        {
            if (_textKeyboard != null) _textKeyboard.onTextInput -= OnTextInput;
            _textKeyboard = null;
        }

        private void RegisterHit(Rect rect, Action<int> click)
        {
            if (Event.current.type == EventType.Repaint)
                _hitTargets.Add(new HitTarget { Bounds = rect, Click = click });
        }

        private void OnDisable() { if (_open) SetOpen(false); }
        private void OnDestroy()
        {
            if (_open) SetOpen(false);
            DetachTextInput();
            if (_harmony != null) _harmony.UnpatchSelf();
            if (_instance == this) _instance = null;
        }

        private void SetOpen(bool value)
        {
            _open = value;
            _hitTargets.Clear();
            _searchFocused = false;
            if (value)
            {
                CaptureGameCursor();
                _cursorWasVisible = Cursor.visible;
                _cursorLockMode = Cursor.lockState;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                _actionsWereEnabled = false;
                try
                {
                    if (GameManager.InputActions != null && GameManager.InputActions.asset != null)
                    {
                        _actionsWereEnabled = GameManager.InputActions.asset.enabled;
                        GameManager.InputActions.Disable();
                    }
                }
                catch (Exception ex) { Logger.LogWarning("Could not pause game input: " + ex.Message); }
                RebuildCatalog();
            }
            else
            {
                DetachTextInput();
                if (_gameCursorImage != null) _gameCursorImage.enabled = _gameCursorImageEnabled;
                _gameCursorImage = null;
                _hardwareMouse = null;
                try { if (_actionsWereEnabled && GameManager.InputActions != null) GameManager.InputActions.Enable(); }
                catch (Exception ex) { Logger.LogWarning("Could not restore game input: " + ex.Message); }
                Cursor.visible = _cursorWasVisible;
                Cursor.lockState = _cursorLockMode;
            }
        }

        private void RebuildCatalog()
        {
            _entries.Clear();
            GameManager manager = GameManager.instance;
            Inventory inventory = manager != null ? manager.inventory : null;
            if (inventory == null)
            {
                _status = "请先载入存档 / Load a save first";
                return;
            }

            SO_GemProperties[] gems =
            {
                inventory.amethystSO, inventory.citrineSO, inventory.quartzSO,
                inventory.aquamarineSO, inventory.peridotSO, inventory.rhodoliteSO,
                inventory.emeraldSO, inventory.morganiteSO, inventory.onyxSO,
                inventory.alexandriteSO, inventory.rubySO, inventory.sapphireSO,
                inventory.bismuthSO, inventory.diamondSO, inventory.opalSO
            };

            foreach (SO_GemProperties gem in gems.Where(g => g != null).Distinct())
            {
                string display = gem.name;
                Color color = RarityColor((int)gem.rarity);
                _entries.Add(new Entry { Kind = ItemKind.BrokenGem, Name = display, SearchText = display + " cracked broken polished 破碎 已擦亮", Icon = gem.refinedGemSprite, Gem = gem, NameColor = color });
                _entries.Add(new Entry { Kind = ItemKind.IntactGem, Name = display, SearchText = display + " intact complete polished 完整 已擦亮", Icon = gem.refinedGemSprite, Gem = gem, NameColor = color });
            }

            ArtifactManager artifacts = manager.artifactManager;
            if (artifacts != null && artifacts.artifactPropertiesList != null)
            {
                foreach (SO_ArtifactProperties artifact in artifacts.artifactPropertiesList.Where(a => a != null))
                {
                    string display;
                    if (artifact.mineLevelDiscoverable == MiningMinigameManager.MineLevel.PoolMine ||
                        !ArtifactNames.TryGetValue(artifact.name, out display)) continue;
                    _entries.Add(new Entry
                    {
                        Kind = ItemKind.Artifact,
                        Name = display,
                        SearchText = display + " " + artifact.name + " artifact ancient 古物",
                        Icon = artifact.unrefinedArtifactSprite,
                        Artifact = artifact,
                        NameColor = ArtifactTierColor((int)artifact.mineLevelDiscoverable)
                    });
                }
            }

            _page = 0;
            _status = "左键 +1 / Right click +10";
        }

        private static Color RarityColor(int rarity)
        {
            switch (rarity)
            {
                case 1: return new Color(0.35f, 0.95f, 0.4f);
                case 2: return new Color(0.35f, 0.68f, 1f);
                case 3: return new Color(0.78f, 0.45f, 1f);
                case 4: return new Color(1f, 0.82f, 0.25f);
                default: return Color.white;
            }
        }

        private static Color ArtifactTierColor(int tier)
        {
            switch (tier)
            {
                case 1: return new Color(0.35f, 0.68f, 1f);
                case 2: return new Color(0.78f, 0.45f, 1f);
                case 4: return new Color(1f, 0.82f, 0.25f);
                default: return Color.white;
            }
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null) return;
            _windowStyle = new GUIStyle(GUI.skin.box);
            _windowStyle.normal.background = MakeTexture(new Color(0.075f, 0.065f, 0.09f, 1f));
            _windowStyle.border = new RectOffset(2, 2, 2, 2);
            _buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 14, wordWrap = true };
            _selectedButtonStyle = new GUIStyle(_buttonStyle);
            _selectedButtonStyle.normal.background = MakeTexture(new Color(0.32f, 0.2f, 0.42f, 1f));
            _nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 13, wordWrap = true };
            _titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 22, fontStyle = FontStyle.Bold };
            _statusStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 13 };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();
            if (Event.current.type == EventType.Repaint) _hitTargets.Clear();

            float width = Mathf.Min(Screen.width - 30f, 1160f);
            float height = Mathf.Min(Screen.height - 30f, 800f);
            Rect panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.Box(panel, GUIContent.none, _windowStyle);

            float sidebar = Mathf.Clamp(width * 0.19f, 170f, 215f);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, width - 36f, 36f), "Gemporium 取物 / Grab Items", _titleStyle);
            DrawCategoryButton(new Rect(panel.x + 16f, panel.y + 66f, sidebar - 28f, 52f), ItemKind.BrokenGem, "破碎宝石（已擦亮）\nPolished / Cracked");
            DrawCategoryButton(new Rect(panel.x + 16f, panel.y + 126f, sidebar - 28f, 52f), ItemKind.IntactGem, "完整宝石（已擦亮）\nPolished / Intact");
            DrawCategoryButton(new Rect(panel.x + 16f, panel.y + 186f, sidebar - 28f, 52f), ItemKind.Artifact, "未擦亮古物\nArtifacts");

            float contentX = panel.x + sidebar;
            float contentWidth = panel.width - sidebar - 18f;
            GUI.Label(new Rect(contentX, panel.y + 52f, 70f, 28f), "搜索 / Search");
            Rect searchRect = new Rect(contentX + 78f, panel.y + 50f, contentWidth - 78f, 30f);
            GUI.Box(searchRect, _search + (_searchFocused ? " |" : ""), GUI.skin.textField);
            RegisterHit(searchRect, button => { if (button == 0) _searchFocused = true; });

            List<Entry> filtered = _entries.Where(e => e.Kind == _category && (string.IsNullOrWhiteSpace(_search) || e.SearchText.IndexOf(_search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            int pages = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)PageSize));
            _page = Mathf.Clamp(_page, 0, pages - 1);

            float gridTop = panel.y + 92f;
            float gridBottom = panel.yMax - 78f;
            float cellWidth = contentWidth / Columns;
            float cellHeight = (gridBottom - gridTop) / Rows;
            int start = _page * PageSize;
            int end = Mathf.Min(start + PageSize, filtered.Count);
            for (int index = start; index < end; index++)
            {
                int slot = index - start;
                int row = slot / Columns;
                int col = slot % Columns;
                DrawEntry(new Rect(contentX + col * cellWidth + 4f, gridTop + row * cellHeight + 4f, cellWidth - 8f, cellHeight - 8f), filtered[index]);
            }

            Rect previous = new Rect(contentX, panel.yMax - 54f, 110f, 32f);
            Rect next = new Rect(contentX + contentWidth - 110f, panel.yMax - 54f, 110f, 32f);
            GUI.enabled = _page > 0;
            GUI.Box(previous, "< 上一页", _buttonStyle);
            if (_page > 0) RegisterHit(previous, button => { if (button == 0) _page--; });
            GUI.enabled = _page + 1 < pages;
            GUI.Box(next, "下一页 >", _buttonStyle);
            if (_page + 1 < pages) RegisterHit(next, button => { if (button == 0) _page++; });
            GUI.enabled = true;
            GUI.Label(new Rect(contentX + contentWidth / 2f - 75f, panel.yMax - 54f, 150f, 32f), (_page + 1) + " / " + pages, _nameStyle);
            GUI.Label(new Rect(panel.x + 16f, panel.yMax - 48f, sidebar - 28f, 24f), _status, _statusStyle);

            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                Vector2 size = GUI.skin.box.CalcSize(new GUIContent(GUI.tooltip));
                GUI.Box(new Rect(_pointer.x + 16f, _pointer.y + 12f, size.x + 18f, size.y + 10f), GUI.tooltip);
            }
            if (Event.current.isMouse && panel.Contains(Event.current.mousePosition)) Event.current.Use();
        }

        private void DrawCategoryButton(Rect rect, ItemKind kind, string label)
        {
            GUI.Box(rect, label, _category == kind ? _selectedButtonStyle : _buttonStyle);
            RegisterHit(rect, button =>
            {
                if (button != 0) return;
                _category = kind;
                _page = 0;
            });
        }

        private void DrawEntry(Rect rect, Entry entry)
        {
            GUI.Box(rect, GUIContent.none);
            Rect iconRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 52f);
            if (entry.Icon != null) DrawSprite(iconRect, entry.Icon);
            else GUI.Label(iconRect, "◆", new GUIStyle(_titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 38, normal = { textColor = entry.NameColor } });

            Color old = GUI.color;
            GUI.color = entry.NameColor;
            GUI.Label(new Rect(rect.x + 4f, rect.yMax - 39f, rect.width - 8f, 34f), new GUIContent(entry.Name, entry.Name), _nameStyle);
            GUI.color = old;

            RegisterHit(rect, button => Add(entry, button == 0 ? 1 : 10));
            if (rect.Contains(_pointer))
            {
                GUI.tooltip = entry.Name;
            }
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            Rect tr;
            try
            {
                tr = sprite.textureRect;
            }
            catch
            {
                Vector2[] points = sprite.uv;
                float minX = points.Min(p => p.x);
                float maxX = points.Max(p => p.x);
                float minY = points.Min(p => p.y);
                float maxY = points.Max(p => p.y);
                tr = new Rect(minX * texture.width, minY * texture.height, (maxX - minX) * texture.width, (maxY - minY) * texture.height);
            }
            Rect uv = new Rect(tr.x / texture.width, tr.y / texture.height, tr.width / texture.width, tr.height / texture.height);
            float scale = Mathf.Min(rect.width / tr.width, rect.height / tr.height);
            Rect fitted = new Rect(rect.x + (rect.width - tr.width * scale) / 2f, rect.y + (rect.height - tr.height * scale) / 2f, tr.width * scale, tr.height * scale);
            GUI.DrawTextureWithTexCoords(fitted, texture, uv, true);
        }

        private void Add(Entry entry, int amount)
        {
            GameManager manager = GameManager.instance;
            Inventory inventory = manager != null ? manager.inventory : null;
            if (inventory == null)
            {
                _status = "库存未就绪 / Inventory unavailable";
                return;
            }

            if (inventory.gemCounter == null || inventory.gemstoneInventoryItemPrefab == null)
            {
                _status = "请在已载入的店铺场景使用 / Use after loading the shop";
                return;
            }

            List<GemstoneInventoryItem> created = new List<GemstoneInventoryItem>();
            List<GemstoneInventoryItem> added = new List<GemstoneInventoryItem>();
            try
            {
                for (int i = 0; i < amount; i++)
                {
                    if (inventory.IsInventoryFull()) throw new InvalidOperationException("Inventory is full; nothing was added.");
                    GemstoneInventoryItem item = inventory.createGemstoneInventoryItem();
                    if (item == null) throw new InvalidOperationException("Could not create an inventory item.");
                    item.transform.SetParent(inventory.transform, false);
                    created.Add(item);
                    if (entry.Kind == ItemKind.Artifact)
                    {
                        item.SetArtifactProperties(entry.Artifact);
                        item.isRefined = false;
                        item.isShiny = false;
                    }
                    else
                    {
                        item.SetGemProperties(entry.Gem, entry.Kind == ItemKind.BrokenGem ? 1 : 0, 0f, true);
                        item.isShiny = false;
                    }
                    inventory.AddToInventory(item);
                    added.Add(item);
                }
                _status = entry.Name + " +" + amount + "（正常保存后持久化）";
            }
            catch (Exception ex)
            {
                foreach (GemstoneInventoryItem item in added)
                {
                    try { inventory.RemoveFromInventory(item); } catch { }
                }
                foreach (GemstoneInventoryItem item in created)
                {
                    if (item != null) Destroy(item.gameObject);
                }
                _status = "未添加 / Nothing added: " + ex.Message;
                Logger.LogError(ex);
            }
        }
    }
}
