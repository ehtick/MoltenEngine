﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Molten.Graphics
{
    public abstract partial class SpriteBatcher
    {
        /// <summary>
        /// Draws a circle with the specified radius.
        /// </summary>
        /// <param name="center">The position of the circle center.</param>
        /// <param name="radius">The radius, in radians.</param>
        /// <param name="startAngle">The start angle of the circle, in radians. This is useful when drawing a partial-circle.</param>
        /// <param name="endAngle">The end angle of the circle, in radians. This is useful when drawing a partial-circle</param>
        /// <param name="col">The color of the circle.</param>
        /// <param name="sides">The number of sides for every 6.28319 radians (360 degrees). A higher value will produce a smoother edge. The minimum value is 3.</param>
        public void DrawCircle(Vector2F center, float radius, float startAngle, float endAngle, Color col, int sides = 16)
        {
            //DrawEllipse(center, radius, radius, startAngle, endAngle, col, sides);
        }

        /// <summary>
        /// Draws a circle with the specified radius.
        /// </summary>
        /// <param name="center">The position of the circle center.</param>
        /// <param name="radius">The radius, in radians.</param>
        /// <param name="col">The color of the circle.</param>
        public void DrawCircle(Vector2F center, float radius, Color col)
        {
            //DrawEllipse(center, radius, radius, col, sides);
        }

        /// <summary>
        /// Draws a circle outline with the specified thickness and radius.
        /// </summary>
        /// <param name="center">The position of the circle center.</param>
        /// <param name="radius">The radius, in radians.</param>
        /// <param name="col">The color of the circle.</param>
        /// <param name="sides">The number of sides for every 6.28319 radians (360 degrees). A higher value will produce a smoother edge. The minimum value is 3.</param>
        /// <param name="thickness">The thickness of the outline.</param>
        public void DrawCircleOutline(Vector2F center, float radius, Color col, int thickness, int sides = 16)
        {
            DrawEllipseOutline(center, radius, radius, col, thickness, sides);
        }

        /// <summary>
        /// Draws a circle outline with the specified thickness and radius.
        /// </summary>
        /// <param name="center">The position of the circle center.</param>
        /// <param name="radius">The radius, in radians.</param>
        /// <param name="startAngle">The start angle of the circle, in radians. This is useful when drawing a partial-circle.</param>
        /// <param name="endAngle">The end angle of the circle, in radians. This is useful when drawing a partial-circle</param>
        /// <param name="col">The color of the circle.</param>
        /// <param name="sides">The number of sides for every 6.28319 radians (360 degrees). A higher value will produce a smoother edge. The minimum value is 3.</param>
        /// <param name="thickness">The thickness of the outline.</param>
        public void DrawCircleOutline(Vector2F center, float radius, float startAngle, float endAngle, Color col, int thickness, int sides = 16, bool outlineCenter = false)
        {
            DrawEllipseOutline(center, radius, radius, startAngle, endAngle, col, thickness, sides, outlineCenter);
        }

        /// <summary>
        /// Draws an ellipse with the specified radius values.
        /// </summary>
        /// <param name="e">The <see cref="Ellipse"/> to be drawn</param>
        /// <param name="color">The color of the ellipse.</param>
        /// <param name="sides">The number of sides for every 6.28319 radians (360 degrees). A higher value will produce a smoother edge. The minimum value is 3.</param>
        public void DrawEllipse(ref Ellipse e, Color color, ITexture2D texture)
        {
            RectangleF bounds = new RectangleF()
            {
                X = e.Center.X - e.RadiusX,
                Y = e.Center.Y - e.RadiusY,
                Width = e.RadiusX * 2,
                Height = e.RadiusY * 2,
            };

            RectangleF src = new RectangleF(0, 0, texture.Width, texture.Height);
            DrawInternal(texture, src, bounds.TopLeft, bounds.Size, color, 0, Vector2F.Zero, null, SpriteFormat.Circle, 0);
        }

        /// <summary>
        /// Draws an ellipse outline with the specified thickness and radius values.
        /// </summary>
        /// <param name="center">The position of the ellipse center.</param>
        /// <param name="xRadius">The X radius, in radians.</param>
        /// <param name="yRadius">The Y radius, in radians.</param>
        /// <param name="color">The color of the ellipse.</param>
        /// <param name="sides">The number of sides for every 6.28319 radians (360 degrees). A higher value will produce a smoother edge. The minimum value is 3.</param>
        /// <param name="thickness">The thickness of the outline.</param>
        public void DrawEllipseOutline(Vector2F center, float xRadius, float yRadius, Color color, int thickness, int sides = 16)
        {
            DrawEllipseOutline(center, xRadius, yRadius, 0 * MathHelper.DegToRad, 360 * MathHelper.DegToRad, color, thickness, sides, false);
        }

        /// <summary>
        /// Draws an ellipse outline with the specified thickness and radius values.
        /// </summary>
        /// <param name="center">The position of the ellipse center.</param>
        /// <param name="xRadius">The X radius, in radians.</param>
        /// <param name="yRadius">The Y radius, in radians.</param>
        /// <param name="startAngle">The start angle of the circle, in radians. This is useful when drawing a partial-ellipse.</param>
        /// <param name="endAngle">The end angle of the circle, in radians. This is useful when drawing a partial-ellipse</param>
        /// <param name="color">The color of the ellipse.</param>
        /// <param name="sides">The number of sides for every 6.28319 radians (360 degrees). A higher value will produce a smoother edge. The minimum value is 3.</param>
        /// <param name="outlineCenter">If true, the outline will also be drawn along the edges toward the center-point, instead of just the outer edge of the ellipse segment.</param>
        /// <param name="thickness">The thickness of the outline.</param>
        public void DrawEllipseOutline(Vector2F center, float xRadius, float yRadius, float startAngle, float endAngle, Color color, int thickness, int sides = 16, bool outlineCenter = false)
        {
            if (sides < CIRCLE_MIN_SIDES)
                throw new SpriteBatcherException(this, $"The minimum number of sides is {CIRCLE_MIN_SIDES}.");

            float angleRange = endAngle - startAngle;
            float inc = angleRange / sides;
            float angle;
            Vector2F pPrev = center;
            Vector2F pFirst = center;

            for(int i = 0; i < sides; i++)
            {
                angle = startAngle + (inc * i);
                Vector2F p = center + new Vector2F()
                {
                    X = (float)Math.Sin(angle) * xRadius,
                    Y = (float)Math.Cos(angle) * yRadius,
                };

                if (i > 0)
                    DrawLine(pPrev, p, color, thickness);
                else
                    pFirst = p;

                pPrev = p;
            }

            if (angleRange < MathHelper.TwoPi)
            {
                if (outlineCenter &&
                    angleRange > 0)
                {
                    DrawLine(pPrev, center, color, thickness);
                    DrawLine(pFirst, center, color, thickness);
                }
            }
            else
            {
                // Connect last point to first point to form a complete ellipse.
                DrawLine(pPrev, pFirst, color, thickness); 
            }
        }
    }
}
