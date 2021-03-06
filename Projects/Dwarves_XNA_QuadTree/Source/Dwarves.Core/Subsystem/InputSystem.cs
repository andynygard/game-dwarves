﻿// ----------------------------------------------------------------------------
// <copyright file="InputSystem.cs" company="Acidwashed Games">
//     Copyright 2012 Acidwashed Games. All right reserved.
// </copyright>
// ----------------------------------------------------------------------------
namespace Dwarves.Subsystem
{
    using System;
    using System.Collections.Generic;
    using Dwarves.Component.Input;
    using Dwarves.Component.Screen;
    using Dwarves.Component.Spatial;
    using EntitySystem;
    using EntitySystem.Subsystem;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    /// <summary>
    /// System responsible for capturing user input.
    /// </summary>
    public class InputSystem : BaseSystem
    {
        /// <summary>
        /// The value for a single mousewheel step.
        /// </summary>
        private const int MousewheelStepValue = 120;

        /// <summary>
        /// The graphics device.
        /// </summary>
        private GraphicsDevice graphics;

        /// <summary>
        /// The previous mouse state.
        /// </summary>
        private MouseState? prevMouseState;

        /// <summary>
        /// The previous entity with focus.
        /// </summary>
        private int? prevEntityWithFocus;

        /// <summary>
        /// Initializes a new instance of the InputSystem class.
        /// </summary>
        /// <param name="entityManager">The EntityManager for the world that this system belongs to.</param>
        /// <param name="graphics">The graphics device.</param>
        public InputSystem(EntityManager entityManager, GraphicsDevice graphics)
            : base(entityManager)
        {
            this.graphics = graphics;
            this.prevMouseState = null;
            this.prevEntityWithFocus = null;
        }

        /// <summary>
        /// Perform the system's processing.
        /// </summary>
        /// <param name="delta">The number of milliseconds since the last processing occurred.</param>
        public override void Process(int delta)
        {
            MouseState mouseState = Mouse.GetState();

            // Check if the screen is currently being touched
            bool isSingleTouch = mouseState.LeftButton == ButtonState.Pressed;

            // Treat right-click as 'multi-touch' and mouse-scroll as 'pinch'
            bool isMultiTouch =
                mouseState.RightButton == ButtonState.Pressed ||
                (this.prevMouseState.HasValue &&
                this.prevMouseState.Value.ScrollWheelValue != mouseState.ScrollWheelValue);

            // Get the camera components, since this needs to be taken into account with determining location
            Entity cameraEntity = this.EntityManager.GetFirstEntityWithComponent(typeof(CameraComponent));
            var cCamera =
                (CameraComponent)this.EntityManager.GetComponent(cameraEntity, typeof(CameraComponent));
            var cCameraPosition =
                (PositionComponent)this.EntityManager.GetComponent(cameraEntity, typeof(PositionComponent));
            var cCameraScale =
                (ScaleComponent)this.EntityManager.GetComponent(cameraEntity, typeof(ScaleComponent));

            int? entityOnTouchPoint = null;
            if (isSingleTouch)
            {
                if (this.prevEntityWithFocus.HasValue)
                {
                    // An entity was touched previously, so keep that one in focus
                    entityOnTouchPoint = this.prevEntityWithFocus;
                }
                else
                {
                    // Get the entities that are touched
                    IEnumerable<Entity> touchedEntities = this.GetEntitiesOnPoint(
                        mouseState.X, mouseState.Y, cCameraScale.Scale, cCameraPosition.Position);

                    // TODO: Pick which entity to focus
                    entityOnTouchPoint = -1;
                }
            }
            else if (isMultiTouch)
            {
                // For multi-touch we camera-pan/zoom. No entities are put into focus with multi-touch though
                entityOnTouchPoint = null;

                // Update the camera position by the mouse delta values
                if (this.prevMouseState.HasValue)
                {
                    float deltaX = mouseState.X - this.prevMouseState.Value.X;
                    float deltaY = mouseState.Y - this.prevMouseState.Value.Y;

                    // Transform from screen coordinates to game world coordinates
                    deltaX *= cCamera.ProjectionWidth / (float)this.graphics.Viewport.Width;
                    deltaY *= cCamera.ProjectionHeight / (float)this.graphics.Viewport.Height;

                    // Transform from game world coordinates to camera-relative coordinates
                    deltaX /= cCameraScale.Scale;
                    deltaY /= cCameraScale.Scale;

                    // Get the number of zoom steps from the mousewheel movement
                    float zoomSteps = mouseState.ScrollWheelValue - this.prevMouseState.Value.ScrollWheelValue;

                    // Normalise the value so one mousewheel-click is one zoom step
                    zoomSteps /= MousewheelStepValue;

                    // Update the camera position and zoom scale
                    cCameraPosition.Position = new Vector2(cCameraPosition.Position.X - deltaX, cCameraPosition.Position.Y + deltaY);
                    cCameraScale.Scale += zoomSteps * cCamera.ZoomStepSize;

                    // Camera zoom can't go below zero
                    if (cCameraScale.Scale <= 0.0f)
                    {
                        cCameraScale.Scale = cCamera.ZoomStepSize;
                    }
                }
            }

            if (entityOnTouchPoint.HasValue && !this.prevEntityWithFocus.HasValue)
            {
                // Touch has began on an entity so set it as focused
                this.SetEntityInputFocus(entityOnTouchPoint.Value, false);
            }
            else if (!entityOnTouchPoint.HasValue && this.prevEntityWithFocus.HasValue)
            {
                // Touch has ended on an entity. Remove the previous entity from focus
                this.SetEntityInputFocus(this.prevEntityWithFocus.Value, false);

                // Since the touch has been released, this is treated as a click on the entity
                this.DoEntityClicked(this.prevEntityWithFocus.Value);
            }

            this.prevMouseState = mouseState;
            this.prevEntityWithFocus = entityOnTouchPoint;
        }

        #region Private Methods

        /// <summary>
        /// Handle a click on an entity.
        /// </summary>
        /// <param name="entity">The entity that was clicked.</param>
        private void DoEntityClicked(int entity)
        {
            // TODO
        }

        /// <summary>
        /// Set the focus of the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="focus">Indicates if the entity is in focus.</param>
        private void SetEntityInputFocus(int entity, bool focus)
        {
            // TODO
        }

        /// <summary>
        /// Gets the entities whose input region contains the given point.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="cameraZoom">The camera zoom ratio.</param>
        /// <param name="cameraPos">The camera position.</param>
        /// <returns>The entities on this point.</returns>
        private IEnumerable<Entity> GetEntitiesOnPoint(int x, int y, float cameraZoom, Vector2 cameraPos)
        {
            var entities = new List<Entity>();

            foreach (Entity entity in this.EntityManager.GetEntitiesWithComponent(typeof(InputRegionComponent)))
            {
                // Get the components
                var cPosition =
                    (PositionComponent)this.EntityManager.GetComponent(entity, typeof(PositionComponent));
                var cInputRegion =
                    (InputRegionComponent)this.EntityManager.GetComponent(entity, typeof(InputRegionComponent));

                // Transform the region from entity-relative coordinates to screen coordinates
                Rectangle rect = cInputRegion.Region;
                if (cPosition.IsScreenCoordinates)
                {
                    // Move the region to the on-screen position
                    rect.X += (int)Math.Round(cPosition.Position.X);
                    rect.Y += (int)Math.Round(cPosition.Position.Y);
                }
                else
                {
                    // Translate the region to the entity's game world position
                    float translateX = cPosition.Position.X;
                    float translateY = cPosition.Position.Y;

                    // Translate the region to the camera-position
                    translateX -= cameraPos.X;
                    translateY -= cameraPos.Y;

                    // Scale the rectangle for the current translation and camera zoom
                    rect.X = (int)Math.Round((((float)rect.X + translateX) * cameraZoom));
                    rect.Y = (int)Math.Round((((float)rect.Y + translateY) * cameraZoom));
                    rect.Width = (int)Math.Round((rect.Width * cameraZoom));
                    rect.Height = (int)Math.Round((rect.Height * cameraZoom));
                }

                // Add the entity if its region contains the given point
                if (rect.Contains(x, y))
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        #endregion
    }
}