type EventMap = {
  // time
  tick: [];
  // sizes
  resize: [];
  // resourceLoader
  ready: [];
  // mouse events
  mouseDown: [MouseEvent];
  mouseMove: [MouseEvent];
  mouseUp: [MouseEvent];
  mouseWheel: [WheelEvent];
  // world events
  switchCamera: [];
  stitchBoxes: [];
  screenshotImage: [];
  resetImage: [];
};

// Weird default export for types....
export default EventMap;
