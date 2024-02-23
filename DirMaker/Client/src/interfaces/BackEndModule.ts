import BackEndDbObject from "./BackEndDbObject";

export default interface BackEndModule {
  Status: Number;
  Progress: Number;
  Message: String;
  ReadyToBuild: BackEndDbObject;
}
