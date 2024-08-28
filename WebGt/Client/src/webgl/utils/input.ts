/* -------------------------------------------------------------------------- */
/*                    Used to handle keyboard input events                    */
/* -------------------------------------------------------------------------- */

import Key from "./types/key";
import EventEmitter from "./eventEmitter";
import EventMap from "./types/eventMap";

export default class Input extends EventEmitter<EventMap> {
  public isWKeyPressed: boolean;
  public isAKeyPressed: boolean;
  public isSKeyPressed: boolean;
  public isDKeyPressed: boolean;
  public isQKeyPressed: boolean;
  public isEKeyPressed: boolean;

  public is1KeyPressed: boolean;
  public is2KeyPressed: boolean;
  public is3KeyPressed: boolean;
  public is4KeyPressed: boolean;

  public isControlLeftPressed: boolean;
  public isSpacePressed: boolean;

  public keys: Key[];

  constructor() {
    super();
    this.isWKeyPressed = false;
    this.isAKeyPressed = false;
    this.isSKeyPressed = false;
    this.isDKeyPressed = false;
    this.isQKeyPressed = false;
    this.isEKeyPressed = false;

    this.is1KeyPressed = false;
    this.is2KeyPressed = false;
    this.is3KeyPressed = false;
    this.is4KeyPressed = false;

    this.isControlLeftPressed = false;
    this.isSpacePressed = false;

    // Define keys
    this.keys = [
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
        keyCode: "KeyQ",
        isPressed: (eventResult: boolean) => {
          this.isQKeyPressed = eventResult;
        },
      },
      {
        keyCode: "KeyE",
        isPressed: (eventResult: boolean) => {
          this.isEKeyPressed = eventResult;
        },
      },
      {
        keyCode: "Digit1",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("switchCamera");
          }

          this.is1KeyPressed = eventResult;
        },
      },
      {
        keyCode: "Digit2",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("stitchBoxes");
          }

          this.is2KeyPressed = eventResult;
        },
      },
      {
        keyCode: "Digit3",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("screenshotImage");
          }

          this.is3KeyPressed = eventResult;
        },
      },
      {
        keyCode: "Digit4",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("resetImage");
          }

          this.is4KeyPressed = eventResult;
        },
      },
      {
        keyCode: "Space",
        isPressed: (eventResult: boolean) => {
          this.isSpacePressed = eventResult;
        },
      },
      {
        keyCode: "ControlLeft",
        isPressed: (eventResult: boolean) => {
          this.isControlLeftPressed = eventResult;
        },
      },
    ];

    /* -------------------------------------------------------------------------- */
    /*                               Event listeners                              */
    /* -------------------------------------------------------------------------- */

    // Keyboard events
    window.addEventListener(
      "keydown",
      (event: KeyboardEvent) => {
        for (const keyIndex in this.keys) {
          if (event.code === this.keys[keyIndex].keyCode) {
            this.keys[keyIndex].isPressed(true);
          }
        }
      },
      false
    );

    window.addEventListener(
      "keyup",
      (event: KeyboardEvent) => {
        for (const keyIndex in this.keys) {
          if (event.code === this.keys[keyIndex].keyCode) {
            this.keys[keyIndex].isPressed(false);
          }
        }
      },
      false
    );

    // Mouse events
    window.addEventListener("mousedown", (event) => {
      this.emit("mouseDown", event);
    });

    window.addEventListener("mouseup", (event) => {
      this.emit("mouseUp", event);
    });

    window.addEventListener("mousemove", (event) => {
      this.emit("mouseMove", event);
    });

    // Window events
    window.addEventListener("wheel", (event) => {
      this.emit("mouseWheel", event);
    });

    // Disable the browser's context menu
    window.addEventListener("contextmenu", (event) => {
      event.preventDefault();
    });

    // Prevent the window scrolling down when using mouse wheel
    window.addEventListener("wheel", (event) => event.preventDefault(), {
      passive: false,
    });
  }

  public destroy() {
    window.addEventListener("keydown", () => {});
    window.addEventListener("keyup", () => {});

    window.addEventListener("mousedown", () => {});
    window.addEventListener("mousemove", () => {});
    window.addEventListener("mouseup", () => {});
    window.addEventListener("wheel", () => {});

    window.addEventListener("contextmenu", () => {});
  }
}
