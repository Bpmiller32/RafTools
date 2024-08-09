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

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "c" || event.key === "C") {
        rectangles.length = 0; // Clear the rectangles array
        graphics.clear(); // Clear the graphics
      } else if (event.key === "p" || event.key === "P") {
        // const cornerCoordinates = cropImage();
        // if (cornerCoordinates) {
        //   const polygonPoints = calculateConvexHull(cornerCoordinates);
        //   drawPolygon(polygonPoints);
        // }

        // drawMergedPolygon();

        drawCroppedImage();

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

    const applyCroppedMaskToTexture = () => {
      if (rectangles.length === 0) return;

      // Create a mask graphics object
      const mask = new PIXI.Graphics();
      mask.beginFill(0xffffff);
      rectangles.forEach((rect) => {
        mask.drawRect(
          (rect.x - sprite.x) / sprite.scale.x + sprite.texture.width / 2,
          (rect.y - sprite.y) / sprite.scale.y + sprite.texture.height / 2,
          rect.width / sprite.scale.x,
          rect.height / sprite.scale.y
        );
      });
      mask.endFill();

      // Create a RenderTexture for the mask
      const renderTexture = PIXI.RenderTexture.create({
        width: sprite.texture.width,
        height: sprite.texture.height,
      });
      app.renderer.render(mask, { renderTexture });

      // Apply the mask to the texture
      const newTexture = new PIXI.Texture(renderTexture);

      // Create a new sprite using the new texture and mask
      const croppedSprite = new PIXI.Sprite(newTexture);
      croppedSprite.anchor.set(0.5);
      croppedSprite.x = sprite.x;
      croppedSprite.y = sprite.y;
      croppedSprite.scale.set(sprite.scale.x, sprite.scale.y);

      // Clear the stage and add the new sprite
      app.stage.removeChildren();
      app.stage.addChild(croppedSprite);

      // Clear the graphics object
      graphics.clear();
    };

    const drawCroppedImage = () => {
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
        width: app.view.width,
        height: app.view.height,
      });
      app.renderer.render(mask, { renderTexture });

      // Create a sprite from the RenderTexture and use it as a mask for the original sprite
      const maskSprite = new PIXI.Sprite(renderTexture);
      sprite.mask = maskSprite;

      // Clear the stage and add the masked sprite
      // app.stage.removeChildren();
      // app.stage.addChild(sprite);

      // Clear the graphics object and add the mask sprite for visualization
      graphics.clear();
      graphics.addChild(maskSprite);
    };

    // const saveCroppedImage = () => {
    //   if (rectangles.length === 0) return;

    //   // Create a RenderTexture for the masked sprite
    //   const renderTexture = PIXI.RenderTexture.create({ width: app.view.width, height: app.view.height });
    //   app.renderer.render(app.stage, { renderTexture });

    //   // Generate a data URL from the RenderTexture
    //   const canvas = PIXI.utils.extract.canvas(app.renderer, renderTexture);
    //   const dataURL = canvas.toDataURL("image/png");

    //   // Create an anchor element and trigger a download
    //   const link = document.createElement("a");
    //   link.href = dataURL;
    //   link.download = "cropped_image.png";
    //   document.body.appendChild(link);
    //   link.click();
    //   document.body.removeChild(link);
    // };

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

    const cropImage = () => {
      if (rectangles.length === 0) return;

      const cornerCoordinates: { x: number; y: number }[] = [];

      // Collect corner coordinates for each rectangle
      rectangles.forEach((rect) => {
        cornerCoordinates.push({ x: rect.x, y: rect.y }); // Top-left corner
        cornerCoordinates.push({ x: rect.x + rect.width, y: rect.y }); // Top-right corner
        cornerCoordinates.push({ x: rect.x, y: rect.y + rect.height }); // Bottom-left corner
        cornerCoordinates.push({
          x: rect.x + rect.width,
          y: rect.y + rect.height,
        }); // Bottom-right corner
      });

      console.log("Corner Coordinates:", cornerCoordinates);

      return cornerCoordinates;
    };

    const drawMergedPolygon = () => {
      if (rectangles.length === 0) return;

      // Clear the graphics object
      graphics.clear();

      // Draw the rectangles as a mask
      const mask = new PIXI.Graphics();
      mask.beginFill(0xffffff);
      rectangles.forEach((rect) => {
        mask.drawRect(rect.x, rect.y, rect.width, rect.height);
      });
      mask.endFill();

      // Apply the mask to the graphics object
      graphics.addChild(mask);
      graphics.mask = mask;

      // Draw the mask on the graphics object
      graphics.beginFill(0xff0000, 0.5);
      rectangles.forEach((rect) => {
        graphics.drawRect(rect.x, rect.y, rect.width, rect.height);
      });
      graphics.endFill();
    };

    const calculateConvexHull = (points: Point[]): Point[] => {
      const hull: Point[] = [];

      // Find the leftmost point
      let leftmost = points[0];
      for (const point of points) {
        if (point.x < leftmost.x) {
          leftmost = point;
        }
      }

      let current = leftmost;
      do {
        hull.push(current);
        let next = points[0];
        for (const point of points) {
          if (next === current || ccw(current, next, point) < 0) {
            next = point;
          }
        }
        current = next;
      } while (current !== leftmost);

      return hull;
    };

    const ccw = (p1: Point, p2: Point, p3: Point): number => {
      return (p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x);
    };

    const createPolygonEdges = (points: Point[]): Point[] => {
      const edges: Edge[] = [];

      // Create edges from points
      rectangles.forEach((rect) => {
        const topLeft = { x: rect.x, y: rect.y };
        const topRight = { x: rect.x + rect.width, y: rect.y };
        const bottomLeft = { x: rect.x, y: rect.y + rect.height };
        const bottomRight = { x: rect.x + rect.width, y: rect.y + rect.height };

        edges.push({ start: topLeft, end: topRight });
        edges.push({ start: topRight, end: bottomRight });
        edges.push({ start: bottomRight, end: bottomLeft });
        edges.push({ start: bottomLeft, end: topLeft });
      });

      // Sort edges into a continuous path
      const path: Point[] = [];
      while (edges.length > 0) {
        if (path.length === 0) {
          path.push(edges[0].start);
          path.push(edges[0].end);
          edges.shift();
        } else {
          const lastPoint = path[path.length - 1];
          for (let i = 0; i < edges.length; i++) {
            if (
              edges[i].start.x === lastPoint.x &&
              edges[i].start.y === lastPoint.y
            ) {
              path.push(edges[i].end);
              edges.splice(i, 1);
              break;
            }
          }
        }
      }

      return path;
    };

    const drawPolygon = (points: Point[]) => {
      graphics.clear();
      graphics.beginFill(0xff0000, 0.5);
      graphics.moveTo(points[0].x, points[0].y);
      for (const point of points) {
        graphics.lineTo(point.x, point.y);
      }
      graphics.lineTo(points[0].x, points[0].y);
      graphics.endFill();
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
