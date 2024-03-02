import BackEndModule from "./BackEndModule";

export default interface BackEndEvent {
  SmartMatch: {
    Crawler: BackEndModule;
    Builder: BackEndModule;
  };
  Parascript: {
    Crawler: BackEndModule;
    Builder: BackEndModule;
  };
}
