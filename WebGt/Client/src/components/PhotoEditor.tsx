import { defineComponent, ref, onMounted } from "vue";
import * as PIXI from "pixi.js";

interface Point {
  x: number;
  y: number;
}

interface Edge {
  start: Point;
  end: Point;
}

interface Rectangle {
  x: number;
  y: number;
  width: number;
  height: number;
}

export default defineComponent({
  setup() {
    const pixiContainer = ref<HTMLElement | null>(null);
    let app: PIXI.Application;
    let sprite: PIXI.Sprite;

    let isPanning = false;
    let isDrawing = false;

    let startPan = { x: 0, y: 0 };
    let startPosition = { x: 0, y: 0 };
    let startDrawPosition = { x: 0, y: 0 };
    // Create a graphics object for drawing bounding boxes
    let debugGraphics = new PIXI.Graphics();
    let graphics: PIXI.Graphics;
    const rectangles: Rectangle[] = [];
    let originalTexture: PIXI.Texture;

    const addEventListeners = () => {
      window.addEventListener("mousemove", onMouseMove);
      window.addEventListener("mouseup", onMouseUp);
      window.addEventListener("keydown", onKeyDown);
      app.canvas.addEventListener("wheel", onWheel);
      app.canvas.addEventListener("mousedown", onMouseDown);
      app.canvas.addEventListener("contextmenu", (e) => e.preventDefault());
    };

    const onWheel = (event: WheelEvent) => {
      event.preventDefault();
      const scaleAmount = event.deltaY > 0 ? 0.9 : 1.1;
      sprite.scale.set(sprite.scale.x * scaleAmount);
      updateGraphicsTransform();
    };

    const onMouseDown = (event: MouseEvent) => {
      if (event.button === 2) {
        // Right mouse button
        isPanning = true;
        startPan = { x: event.clientX, y: event.clientY };
        startPosition = { x: sprite.x, y: sprite.y };
      } else if (event.button === 0) {
        // Left mouse button
        isDrawing = true;
        startDrawPosition = { x: event.clientX, y: event.clientY };
      }
    };

    const onMouseMove = (event: MouseEvent) => {
      if (isPanning) {
        const dx = event.clientX - startPan.x;
        const dy = event.clientY - startPan.y;
        sprite.x = startPosition.x + dx;
        sprite.y = startPosition.y + dy;
        updateGraphicsTransform();
      } else if (isDrawing) {
        const currentPos = { x: event.clientX, y: event.clientY };
        const x = Math.min(currentPos.x, startDrawPosition.x);
        const y = Math.min(currentPos.y, startDrawPosition.y);
        const width = Math.abs(currentPos.x - startDrawPosition.x);
        const height = Math.abs(currentPos.y - startDrawPosition.y);
        graphics.clear();
        renderRectangles();
        graphics.beginFill(0x66ccff, 0.5);
        graphics.drawRect(
          x - app.canvas.getBoundingClientRect().left,
          y - app.canvas.getBoundingClientRect().top,
          width,
          height
        );
        graphics.endFill();
      }
    };

    const onMouseUp = (event: MouseEvent) => {
      if (isDrawing) {
        const currentPos = { x: event.clientX, y: event.clientY };
        const x = Math.min(currentPos.x, startDrawPosition.x);
        const y = Math.min(currentPos.y, startDrawPosition.y);
        const width = Math.abs(currentPos.x - startDrawPosition.x);
        const height = Math.abs(currentPos.y - startDrawPosition.y);
        rectangles.push({
          x: x - app.canvas.getBoundingClientRect().left,
          y: y - app.canvas.getBoundingClientRect().top,
          width,
          height,
        });
        isDrawing = false;
        graphics.clear();
        renderRectangles();
      }
      isPanning = false;
    };

    const onKeyDown = async (event: KeyboardEvent) => {
      if (event.key === "c" || event.key === "C") {
        rectangles.length = 0; // Clear the rectangles array
        graphics.clear(); // Clear the graphics
        sprite.mask = null;
      } else if (event.key === "p" || event.key === "P") {
        // const cornerCoordinates = cropImage();
        // if (cornerCoordinates) {
        //   const polygonPoints = calculateConvexHull(cornerCoordinates);
        //   drawPolygon(polygonPoints);
        // }

        // drawMergedPolygon();

        await drawCroppedImage();

        // applyCroppedMaskToTexture();
      } else if (event.key === "s" || event.key === "S") {
        saveCroppedImage();
      }
    };

    // Function to draw bounding boxes around all sprites
    function drawBoundingBoxes(stage: PIXI.Container) {
      debugGraphics.clear(); // Clear previous drawings
      debugGraphics.lineStyle(2, 0x00ff00, 1); // Green color with 2px width

      stage.children.forEach((child) => {
        if (child instanceof PIXI.Sprite) {
          const bounds = child.getBounds();
          debugGraphics.drawRect(
            bounds.x,
            bounds.y,
            bounds.width,
            bounds.height
          );
        }
      });
    }

    const saveCroppedImage = () => {
      // Create a RenderTexture for the current stage
      const renderTexture = PIXI.RenderTexture.create({
        width: app.view.width,
        height: app.view.height,
      });

      // Render the current stage to the RenderTexture
      app.renderer.render(app.stage, { renderTexture });

      const canvas = app.renderer.extract.canvas(
        renderTexture
      ) as HTMLCanvasElement;

      const dataUrl = canvas.toDataURL("image/png")!;

      // Create an anchor element and trigger a download
      const link = document.createElement("a");
      link.href = dataUrl;
      link.download = "cropped_image.png";
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    };

    const drawCroppedImage = async () => {
      if (rectangles.length === 0) return;

      // Create a mask graphics object
      const mask = new PIXI.Graphics();
      mask.beginFill(0xffffff);
      rectangles.forEach((rect) => {
        mask.drawRect(rect.x, rect.y, rect.width, rect.height);
      });
      mask.endFill();

      // Create a RenderTexture for the mask
      const renderTexture = PIXI.RenderTexture.create({
        width: app.canvas.width,
        height: app.canvas.height,
      });

      // app.renderer.render(mask, { renderTexture });
      app.renderer.render({
        container: mask,
        target: renderTexture,
      });

      // Create a sprite from the RenderTexture and use it as a mask for the original sprite
      const maskSprite = new PIXI.Sprite(renderTexture);
      sprite.mask = maskSprite;

      graphics.clear();

      // rectangles.length = 0;
      // sprite.mask = null;
      // sprite.texture = screenshotTexture;

      // Remove all elements from the stage
      // app.stage.removeChildren();

      // // Create a sprite from the texture and add it to the stage
      // const screenshotSprite = new PIXI.Sprite(screenshotTexture);
      // screenshotSprite.x = app.canvas.width / 2;
      // screenshotSprite.y = app.canvas.height / 2;
      // screenshotSprite.anchor.set(0.5);

      // app.stage.addChild(screenshotSprite);
    };

    const updateGraphicsTransform = () => {
      graphics.x = sprite.x;
      graphics.y = sprite.y;
      graphics.scale.set(sprite.scale.x, sprite.scale.y);
    };

    const renderRectangles = () => {
      rectangles.forEach((rect) => {
        graphics.beginFill(0x66ccff, 0.5);
        graphics.drawRect(rect.x, rect.y, rect.width, rect.height);
        graphics.endFill();
      });
    };

    onMounted(async () => {
      if (pixiContainer.value) {
        // Initialize WebGl Pixi
        app = new PIXI.Application();
        await app.init({ width: 640, height: 360 });
        pixiContainer.value.appendChild(app.canvas);

        // Load an image
        originalTexture = await PIXI.Assets.load("/assets/example.jpg");
        sprite = PIXI.Sprite.from(originalTexture);

        // Calculate the scale factors
        const scaleX = app.canvas.width / sprite.texture.width;
        const scaleY = app.canvas.height / sprite.texture.height;
        const scale = Math.min(scaleX, scaleY);

        sprite.scale.set(scale);

        // Center the sprite's anchor point
        sprite.anchor.set(0.5);

        // Move the sprite to the center of the screen
        sprite.x = app.screen.width / 2;
        sprite.y = app.screen.height / 2;

        // Add image
        app.stage.addChild(sprite);

        // Add a graphics object for drawing
        graphics = new PIXI.Graphics();
        debugGraphics = new PIXI.Graphics();
        app.stage.addChild(graphics);
        app.stage.addChild(debugGraphics);

        addEventListeners();

        // Update loop
        app.ticker.add(() => {
          drawBoundingBoxes(app.stage);
        });
      }
    });

    return () => (
      <>
        <div
          ref={pixiContainer}
          class="w-[800px] h-[600px]"
          onContextmenu={(e) => e.preventDefault()}
        ></div>
      </>
    );
  },
});
