type EventMap = {
  // time
  tick: [];
  // sizes
  resize: [];
  // resourceLoader
  appReady: [];
  loadedFromApi: [];
  // mouse events
  mouseDown: [MouseEvent];
  mouseMove: [MouseEvent];
  mouseUp: [MouseEvent];
  mouseWheel: [WheelEvent];
  lockPointer: [boolean];
  // world events
  switchCamera: [];
  stitchBoxes: [];
  screenshotImage: [];
  resetImage: [];
};

// Weird default export for types....
export default EventMap;