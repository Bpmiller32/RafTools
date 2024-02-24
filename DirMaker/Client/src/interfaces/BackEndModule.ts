import BackEndDbEntry from "./BackEndDbEntry";

export default interface BackEndModule {
  Status: Number;
  Progress: Number;
  Message: String;
  ReadyToBuild: BackEndDbEntry;
}
