using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.UI;
using StardewValley;
using XRectangle = xTile.Dimensions.Rectangle;

namespace Pathoschild.Stardew.DataMaps.Framework
{
    /// <summary>Renders a data map as an overlay over the world.</summary>
    internal class DataMapOverlay : BaseOverlay
    {
        /*********
        ** Properties
        *********/
        /// <summary>The pixel padding between the color box and its label.</summary>
        private readonly int LegendColorPadding = 5;

        /// <summary>The size of the margin around the displayed legend.</summary>
        private readonly int Margin = 30;

        /// <summary>The padding between the border and content.</summary>
        private readonly int Padding = 5;

        /// <summary>The pixel size of a color box in the legend.</summary>
        private readonly int LegendColorSize;

        /// <summary>The width of the top-left boxes.</summary>
        private readonly int BoxContentWidth;

        /// <summary>Get whether the overlay should be drawn.</summary>
        private readonly Func<bool> DrawOverlay;

        /// <summary>The available data maps.</summary>
        private readonly IDataMap[] Maps;

        /// <summary>When two groups of the same color overlap, draw one border around their edges instead of their individual borders.</summary>
        private readonly bool CombineOverlappingBorders;

        /// <summary>The current data map to render.</summary>
        private IDataMap CurrentMap;

        /// <summary>The legend entries to show.</summary>
        private LegendEntry[] Legend;

        /// <summary>The tiles to render.</summary>
        private TileGroup[] TileGroups;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="maps">The data maps to render.</param>
        /// <param name="drawOverlay">Get whether the overlay should be drawn.</param>
        /// <param name="combineOverlappingBorders">When two groups of the same color overlap, draw one border around their edges instead of their individual borders.</param>
        public DataMapOverlay(IDataMap[] maps, Func<bool> drawOverlay, bool combineOverlappingBorders)
        {
            if (!maps.Any())
                throw new InvalidOperationException("Can't initialise the data maps overlay with no data maps.");

            this.Maps = maps.OrderBy(p => p.Name).ToArray();
            this.DrawOverlay = drawOverlay;
            this.LegendColorSize = (int)Game1.smallFont.MeasureString("X").Y;
            this.BoxContentWidth = this.GetMaxContentWidth(this.Maps, this.LegendColorSize);
            this.CombineOverlappingBorders = combineOverlappingBorders;
            this.SetMap(this.Maps.First());
        }

        /// <summary>Switch to the next data map.</summary>
        public void NextMap()
        {
            int index = Array.IndexOf(this.Maps, this.CurrentMap) + 1;
            if (index >= this.Maps.Length)
                index = 0;
            this.SetMap(this.Maps[index]);
        }

        /// <summary>Switch to the previous data map.</summary>
        public void PrevMap()
        {
            int index = Array.IndexOf(this.Maps, this.CurrentMap) - 1;
            if (index < 0)
                index = this.Maps.Length - 1;
            this.SetMap(this.Maps[index]);
        }

        /// <summary>Update the overlay.</summary>
        public void Update()
        {
            // no tiles to draw
            if (Game1.currentLocation == null || this.CurrentMap == null)
            {
                this.TileGroups = new TileGroup[0];
                return;
            }

            // get updated tiles
            GameLocation location = Game1.currentLocation;
            Vector2 cursorTile = TileHelper.GetTileFromCursor();
            this.TileGroups = this.CurrentMap.Update(location, this.GetVisibleArea(Game1.viewport), cursorTile).ToArray();
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Draw to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        protected override void Draw(SpriteBatch spriteBatch)
        {
            if (!this.DrawOverlay())
                return;

            // collect tile details
            TileDrawData[] tiles = this.AggregateTileData(this.TileGroups, this.CombineOverlappingBorders).ToArray();

            // draw
            int tileSize = Game1.tileSize;
            const int borderSize = 4;
            foreach (TileDrawData tile in tiles)
            {
                Vector2 pixelPosition = tile.TilePosition * tileSize - new Vector2(Game1.viewport.X, Game1.viewport.Y);

                // overlay
                foreach (Color color in tile.Colors)
                    spriteBatch.Draw(CommonHelper.Pixel, new Rectangle((int)pixelPosition.X, (int)pixelPosition.Y, tileSize, tileSize), color * .3f);

                // borders
                foreach (Color color in tile.BorderColors.Keys)
                {
                    TileEdge edges = tile.BorderColors[color];
                    if (edges.HasFlag(TileEdge.Left))
                        spriteBatch.Draw(CommonHelper.Pixel, new Rectangle((int)pixelPosition.X, (int)pixelPosition.Y, borderSize, tileSize), color);
                    if (edges.HasFlag(TileEdge.Right))
                        spriteBatch.Draw(CommonHelper.Pixel, new Rectangle((int)(pixelPosition.X + tileSize - borderSize), (int)pixelPosition.Y, borderSize, tileSize), color);
                    if (edges.HasFlag(TileEdge.Top))
                        spriteBatch.Draw(CommonHelper.Pixel, new Rectangle((int)pixelPosition.X, (int)pixelPosition.Y, tileSize, borderSize), color);
                    if (edges.HasFlag(TileEdge.Bottom))
                        spriteBatch.Draw(CommonHelper.Pixel, new Rectangle((int)pixelPosition.X, (int)(pixelPosition.Y + tileSize - borderSize), tileSize, borderSize), color);
                }
            }

            // draw top-left boxes
            {
                // calculate dimensions
                int topOffset = this.Margin;
                int leftOffset = this.Margin;

                // draw overlay label
                {
                    Vector2 labelSize = Game1.smallFont.MeasureString(this.CurrentMap.Name);
                    CommonHelper.DrawScroll(spriteBatch, new Vector2(leftOffset, topOffset), new Vector2(this.BoxContentWidth, labelSize.Y), out Vector2 contentPos, out Rectangle bounds);

                    contentPos = contentPos + new Vector2((this.BoxContentWidth - labelSize.X) / 2, 0); // center label in box
                    spriteBatch.DrawString(Game1.smallFont, this.CurrentMap.Name, contentPos, Color.Black);

                    topOffset += bounds.Height + this.Padding;
                }

                // draw legend
                if (this.Legend.Any())
                {
                    CommonHelper.DrawScroll(spriteBatch, new Vector2(leftOffset, topOffset), new Vector2(this.BoxContentWidth, this.Legend.Length * this.LegendColorSize), out Vector2 contentPos, out Rectangle _);
                    for (int i = 0; i < this.Legend.Length; i++)
                    {
                        LegendEntry value = this.Legend[i];
                        int legendX = (int)contentPos.X;
                        int legendY = (int)(contentPos.Y + i * this.LegendColorSize);

                        spriteBatch.DrawLine(legendX, legendY, new Vector2(this.LegendColorSize), value.Color);
                        spriteBatch.DrawString(Game1.smallFont, value.Name, new Vector2(legendX + this.LegendColorSize + this.LegendColorPadding, legendY + 2), Color.Black);
                    }
                }
            }
        }

        /// <summary>Aggregate tile data to draw.</summary>
        /// <param name="groups">The tile groups to draw.</param>
        /// <param name="combineOverlappingBorders">When two groups of the same color overlap, draw one border around their edges instead of their individual borders.</param>
        private IEnumerable<TileDrawData> AggregateTileData(IEnumerable<TileGroup> groups, bool combineOverlappingBorders)
        {
            // collect tile details
            IDictionary<Vector2, TileDrawData> tiles = new Dictionary<Vector2, TileDrawData>();
            foreach (TileGroup group in groups)
            {
                Lazy<HashSet<Vector2>> inGroupLazy = new Lazy<HashSet<Vector2>>(() => new HashSet<Vector2>(group.Tiles.Select(p => p.TilePosition)));
                foreach (TileData groupTile in group.Tiles)
                {
                    // get tile data
                    Vector2 position = groupTile.TilePosition;
                    if (!tiles.TryGetValue(position, out TileDrawData data))
                        data = tiles[position] = new TileDrawData(position);

                    // update data
                    data.Colors.Add(groupTile.Color);
                    if (group.OuterBorderColor.HasValue && !data.BorderColors.ContainsKey(group.OuterBorderColor.Value))
                        data.BorderColors[group.OuterBorderColor.Value] = TileEdge.None; // we'll detect combined borders next

                    // detect borders (if not combined)
                    if (!combineOverlappingBorders && group.OuterBorderColor.HasValue)
                    {
                        Color borderColor = group.OuterBorderColor.Value;
                        int x = (int)groupTile.TilePosition.X;
                        int y = (int)groupTile.TilePosition.Y;
                        HashSet<Vector2> inGroup = inGroupLazy.Value;

                        TileEdge edge = data.BorderColors[borderColor];
                        if (!inGroup.Contains(new Vector2(x - 1, y)))
                            edge |= TileEdge.Left;
                        if (!inGroup.Contains(new Vector2(x + 1, y)))
                            edge |= TileEdge.Right;
                        if (!inGroup.Contains(new Vector2(x, y - 1)))
                            edge |= TileEdge.Top;
                        if (!inGroup.Contains(new Vector2(x, y + 1)))
                            edge |= TileEdge.Bottom;
                        data.BorderColors[borderColor] = edge;
                    }
                }
            }

            // detect color borders
            if (combineOverlappingBorders)
            {
                foreach (Vector2 position in tiles.Keys)
                {
                    // get tile
                    int x = (int)position.X;
                    int y = (int)position.Y;
                    TileDrawData data = tiles[position];
                    if (!data.BorderColors.Any())
                        continue;

                    // get neighbours
                    tiles.TryGetValue(new Vector2(x - 1, y), out TileDrawData left);
                    tiles.TryGetValue(new Vector2(x + 1, y), out TileDrawData right);
                    tiles.TryGetValue(new Vector2(x, y - 1), out TileDrawData top);
                    tiles.TryGetValue(new Vector2(x, y + 1), out TileDrawData bottom);

                    // detect edges
                    foreach (Color color in data.BorderColors.Keys.ToArray())
                    {
                        if (left == null || !left.BorderColors.ContainsKey(color))
                            data.BorderColors[color] |= TileEdge.Left;
                        if (right == null || !right.BorderColors.ContainsKey(color))
                            data.BorderColors[color] |= TileEdge.Right;
                        if (top == null || !top.BorderColors.ContainsKey(color))
                            data.BorderColors[color] |= TileEdge.Top;
                        if (bottom == null || !bottom.BorderColors.ContainsKey(color))
                            data.BorderColors[color] |= TileEdge.Bottom;
                    }
                }
            }

            return tiles.Values;
        }

        /// <summary>Switch to the given data map.</summary>
        /// <param name="map">The data map to select.</param>
        private void SetMap(IDataMap map)
        {
            this.CurrentMap = map;
            this.Legend = this.CurrentMap.Legend.ToArray();
            this.TileGroups = new TileGroup[0];
        }

        /// <summary>Get the tile area currently visible to the player.</summary>
        /// <param name="viewport">The game viewport.</param>
        private Rectangle GetVisibleArea(XRectangle viewport)
        {
            int tileSize = Game1.tileSize;
            int left = viewport.X / tileSize;
            int top = viewport.Y / tileSize;
            int width = (int)Math.Ceiling(viewport.Width / (decimal)tileSize);
            int height = (int)Math.Ceiling(viewport.Height / (decimal)tileSize);

            return new Rectangle(left - 1, top - 1, width + 2, height + 2); // extend slightly off-screen to avoid tile pop-in at the edges
        }

        /// <summary>Get the maximum content width needed to render the data map labels and legends.</summary>
        /// <param name="maps">The data maps to render.</param>
        /// <param name="legendColorSize">The pixel size of a color box in the legend.</param>
        private int GetMaxContentWidth(IDataMap[] maps, int legendColorSize)
        {
            float labelWidth =
                (
                    from map in maps
                    select Game1.smallFont.MeasureString(map.Name).X
                )
                .Max();
            float legendContentWidth =
                (
                    from map in maps
                    from entry in map.Legend
                    select Game1.smallFont.MeasureString(entry.Name).X
                )
                .Max() + legendColorSize + this.LegendColorPadding;

            return (int)Math.Max(labelWidth, legendContentWidth);
        }
    }
}
