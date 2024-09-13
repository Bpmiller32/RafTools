/* -------------------------------------------------------------------------- */
/*             Typescript event emitter using Mitt and events list            */
/* -------------------------------------------------------------------------- */

import mitt from "mitt";

type EventMap = {
  // app state
  startApp: void;
  appReady: void;
  appError: void;
  // time
  tick: void;
  // sizes
  resize: void;
  // mouse events
  mouseDown: MouseEvent;
  mouseMove: MouseEvent;
  mouseUp: MouseEvent;
  mouseWheel: WheelEvent;
  lockPointer: boolean;
  // world events
  switchCamera: void;
  stitchBoxes: void;
  screenshotImage: void;
  resetImage: void;
  // api events
  loadedFromApi: void;
  fillInForm: void;
  gotoNextImage: void;
};

// Create an emitter instance
const Emitter = mitt<EventMap>();

export default Emitter;
