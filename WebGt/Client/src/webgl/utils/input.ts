/* -------------------------------------------------------------------------- */
/*                    Used to handle keyboard input events                    */
/* -------------------------------------------------------------------------- */

import * as THREE from "three";
import Experience from "../experience";
import Key from "./types/key";
import EventEmitter from "./eventEmitter";
import EventMap from "./types/eventMap";

export default class Input extends EventEmitter<EventMap> {
  private experience: Experience;

  public isWKeyPressed: boolean;
  public isAKeyPressed: boolean;
  public isSKeyPressed: boolean;
  public isDKeyPressed: boolean;
  public isCKeyPressed: boolean;
  public isPKeyPressed: boolean;

  public keys: Key[];
  public cameraTargetPosition: THREE.Vector3;
  public clickStartPoint: THREE.Vector3;
  public clickEndPoint: THREE.Vector3;
  public interpolationSmoothness: number;

  private isDraggingLeftClick: boolean;
  private isDraggingRightClick: boolean;
  private previousMousePosition: THREE.Vector2;
  private dragSensitivity: number;
  private scrollSensitivity: number;

  constructor() {
    super();

    this.experience = Experience.getInstance();

    this.isWKeyPressed = false;
    this.isAKeyPressed = false;
    this.isSKeyPressed = false;
    this.isDKeyPressed = false;

    this.isCKeyPressed = false;
    this.isPKeyPressed = false;

    this.keys = [
      // WASD
      {
        keyCode: "KeyW",
        isPressed: (eventResult: boolean) => {
          this.isWKeyPressed = eventResult;
        },
      },
      {
        keyCode: "KeyA",
        isPressed: (eventResult: boolean) => {
          this.isAKeyPressed = eventResult;
        },
      },
      {
        keyCode: "KeyS",
        isPressed: (eventResult: boolean) => {
          this.isSKeyPressed = eventResult;
        },
      },
      {
        keyCode: "KeyD",
        isPressed: (eventResult: boolean) => {
          this.isDKeyPressed = eventResult;
        },
      },

      {
        keyCode: "KeyC",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("stitchBoxes");
          }
        },
      },
      {
        keyCode: "KeyP",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("screenshotImage");
          }
        },
      },
    ];

    this.cameraTargetPosition = new THREE.Vector3();
    this.clickStartPoint = new THREE.Vector3();
    this.clickEndPoint = new THREE.Vector3();
    this.interpolationSmoothness = 0.1;

    this.isDraggingLeftClick = false;
    this.isDraggingRightClick = false;
    this.previousMousePosition = new THREE.Vector2(0, 0);
    this.dragSensitivity = 0.01;
    this.scrollSensitivity = 0.01;

    // Event listeners

    // Keys
    window.addEventListener(
      "keydown",
      (event: KeyboardEvent) => {
        this.onKeyDown(event.code);
      },
      false
    );

    window.addEventListener(
      "keyup",
      (event: KeyboardEvent) => {
        this.onKeyUp(event.code);
      },
      false
    );

    // Mouse
    window.addEventListener("mousedown", (event) => {
      if (event.button === 0) {
        this.isDraggingLeftClick = true;
        this.clickStartPoint = this.getMousePosition(
          event.clientX,
          event.clientY
        );

        this.emit("mouseDown");
      }

      if (event.button === 2) {
        this.isDraggingRightClick = true;
        this.previousMousePosition.x = event.clientX;
        this.previousMousePosition.y = event.clientY;
      }
    });

    window.addEventListener("mouseup", (event) => {
      if (event.button === 0 && this.isDraggingLeftClick) {
        this.isDraggingLeftClick = false;
        this.clickEndPoint = this.getMousePosition(
          event.clientX,
          event.clientY
        );

        this.emit("mouseUp");
      }

      if (event.button === 2 && this.isDraggingRightClick) {
        this.isDraggingRightClick = false;
      }
    });

    window.addEventListener("mousemove", (event) => {
      if (!this.isDraggingLeftClick && !this.isDraggingRightClick) {
        return;
      }

      if (this.isDraggingLeftClick) {
        this.clickEndPoint = this.getMousePosition(
          event.clientX,
          event.clientY
        );

        this.emit("mouseMove");
      }

      if (this.isDraggingRightClick) {
        const deltaMove = new THREE.Vector2(
          event.clientX - this.previousMousePosition.x,
          event.clientY - this.previousMousePosition.y
        );

        this.cameraTargetPosition.x -= deltaMove.x * this.dragSensitivity;
        this.cameraTargetPosition.y += deltaMove.y * this.dragSensitivity;

        this.previousMousePosition.x = event.clientX;
        this.previousMousePosition.y = event.clientY;
      }
    });

    window.addEventListener("wheel", (event) => {
      const delta = event.deltaY * this.scrollSensitivity;
      this.cameraTargetPosition.z += delta;
    });

    // Disable the browser's context menu
    window.addEventListener("contextmenu", (event) => {
      event.preventDefault();
    });
  }

  private onKeyDown(keyName: string) {
    for (const keyIndex in this.keys) {
      if (keyName == this.keys[keyIndex].keyCode) {
        this.keys[keyIndex].isPressed(true);
      }
    }
  }

  private onKeyUp(keyName: string) {
    for (const keyIndex in this.keys) {
      if (keyName == this.keys[keyIndex].keyCode) {
        this.keys[keyIndex].isPressed(false);
      }
    }
  }

  private getMousePosition(x: number, y: number) {
    const rect = this.experience.targetElement!.getBoundingClientRect();
    return new THREE.Vector3(
      ((x - rect.left) / rect.width) * 2 - 1,
      -((y - rect.top) / rect.height) * 2 + 1,
      0.5
    ).unproject(this.experience.camera.instance);
  }

  public destroy() {
    window.addEventListener("keydown", () => {});
    window.addEventListener("keyup", () => {});

    window.addEventListener("mouseup", () => {});
    window.addEventListener("mousedown", () => {});
    window.addEventListener("mousemove", () => {});
    window.addEventListener("wheel", () => {});

    window.addEventListener("contextmenu", () => {});
  }
}
