/* -------------------------------------------------------------------------- */
/*               Used to handle keyboard and mouse input events               */
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

  public is0KeyPressed: boolean;
  public is1KeyPressed: boolean;
  public is2KeyPressed: boolean;
  public is3KeyPressed: boolean;
  public is4KeyPressed: boolean;

  public isControlLeftPressed: boolean;
  public isSpacePressed: boolean;
  public isShiftLeftPressed: boolean;

  public isLeftClickPressed: boolean;
  public isRightClickPressed: boolean;

  public dashboardGuiGlobal: HTMLElement | null;
  public loginGuiGlobal: HTMLElement | null;
  public dashboardTextarea: HTMLTextAreaElement | null;
  public isInteractingWithGui: boolean;

  public keys: Key[];

  constructor() {
    super();

    this.isWKeyPressed = false;
    this.isAKeyPressed = false;
    this.isSKeyPressed = false;
    this.isDKeyPressed = false;
    this.isQKeyPressed = false;
    this.isEKeyPressed = false;

    this.is0KeyPressed = false;
    this.is1KeyPressed = false;
    this.is2KeyPressed = false;
    this.is3KeyPressed = false;
    this.is4KeyPressed = false;

    this.isControlLeftPressed = false;
    this.isSpacePressed = false;
    this.isShiftLeftPressed = false;

    this.isLeftClickPressed = false;
    this.isRightClickPressed = false;

    this.dashboardGuiGlobal = document.getElementById("gui");
    this.loginGuiGlobal = document.getElementById("loginPage");
    this.dashboardTextarea = document.getElementById(
      "guiTextArea"
    ) as HTMLTextAreaElement;
    this.isInteractingWithGui = false;

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
        keyCode: "Digit0",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("switchCamera");
          }

          this.is0KeyPressed = eventResult;
        },
      },
      {
        keyCode: "Digit1",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("stitchBoxes");
          }

          this.is1KeyPressed = eventResult;
        },
      },
      {
        keyCode: "Digit2",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("screenshotImage");
          }

          this.is2KeyPressed = eventResult;
        },
      },
      {
        keyCode: "Digit3",
        isPressed: (eventResult: boolean) => {
          if (eventResult) {
            this.emit("resetImage");
          }

          this.is3KeyPressed = eventResult;
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
      {
        keyCode: "ShiftLeft",
        isPressed: (eventResult: boolean) => {
          this.emit("lockPointer", eventResult);

          this.isShiftLeftPressed = eventResult;
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
      if (event.button === 1) {
        this.isLeftClickPressed = true;
      }
      if (event.button === 2) {
        this.isRightClickPressed = true;
      }

      this.emit("mouseDown", event);
    });

    window.addEventListener("mousemove", (event) => {
      this.emit("mouseMove", event);
    });

    window.addEventListener("mouseup", (event) => {
      if (event.button === 1) {
        this.isLeftClickPressed = false;
      }
      if (event.button === 2) {
        this.isRightClickPressed = false;
      }

      this.emit("mouseUp", event);
    });

    window.addEventListener("wheel", (event) => {
      this.emit("mouseWheel", event);
    });

    // Window events
    // Disable the browser's context menu (enables prefered right click behavior)
    // TODO: reenable after debugging dashboard
    window.addEventListener("contextmenu", (event) => {
      event.preventDefault();
    });

    // Prevent the window scrolling down when using mouse wheel
    window.addEventListener("wheel", (event) => event.preventDefault(), {
      passive: false,
    });

    // GUI events
    this.dashboardGuiGlobal?.addEventListener("mousedown", () => {
      this.isInteractingWithGui = true;
    });
    this.dashboardGuiGlobal?.addEventListener("mouseup", () => {
      this.isInteractingWithGui = false;
    });
    this.loginGuiGlobal?.addEventListener("mousedown", () => {
      this.isInteractingWithGui = true;
    });
    this.loginGuiGlobal?.addEventListener("mouseup", () => {
      this.isInteractingWithGui = false;
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
