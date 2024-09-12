/* -------------------------------------------------------------------------- */
/*             Typescript event emitter using Mitt and events list            */
/* -------------------------------------------------------------------------- */

import mitt from "mitt";

type EventMap = {
  // time
  tick: void;
  // sizes
  resize: void;
  // resourceLoader
  appStarted: void;
  appReady: void;
  loadedFromApi: void;
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
  fillInForm: void;
  gotoNextImage: void;
};

// Create an emitter instance
const Emitter = mitt<EventMap>();

export default Emitter;
