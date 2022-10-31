import { defineStore } from "pinia";

export const useStore = defineStore("main", () => {
  // Websocket connections
  let connectionCrawler = null;
  let connectionBuilder = null;
  let connectionTester = null;
  let test = null;

  // Service stores
  let crawlers = {
    SmartMatch: {
      DataRecieved: false,

      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,
      AvailableBuilds: [],
    },
    Parascript: {
      DataRecieved: false,

      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,
      AvailableBuilds: [],
    },
    RoyalMail: {
      DataRecieved: false,

      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,
      AvailableBuilds: [],
    },
  };
  let builders = {
    SmartMatch: {
      DataRecieved: false,

      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,
      CompiledBuilds: [],
      CurrentBuild: null,
      Progress: null,
    },
    Parascript: {
      DataRecieved: false,

      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,
      CompiledBuilds: [],
      CurrentBuild: null,
      Progress: null,
    },
    RoyalMail: {
      DataRecieved: false,

      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,
      CompiledBuilds: [],
      CurrentBuild: null,
      Progress: null,
    },
  };
  let testers = {
    SmartMatch: {
      DataRecieved: false,

      DirectoryStatus: null,
      Progress: null,
    },
    Zip4: {
      DataRecieved: false,

      DirectoryStatus: null,
      Progress: null,
    },
    Parascript: {
      DataRecieved: false,

      DirectoryStatus: null,
      Progress: null,
    },
    RoyalMail: {
      DataRecieved: false,

      DirectoryStatus: null,
      Progress: null,
    },
  };

  function SendMessage(Directory, Property, Value) {
    this.connectionCrawler.send(JSON.stringify({ Directory, Property, Value }));
  }
  function SendMessageBuilder(Directory, Property, Value) {
    this.connectionBuilder.send(JSON.stringify({ Directory, Property, Value }));
  }
  function SendMessageTester(Directory, Property, Value) {
    this.connectionTester.send(JSON.stringify({ Directory, Property, Value }));
  }

  return {
    connectionCrawler,
    connectionBuilder,
    connectionTester,
    crawlers,
    builders,
    testers,
    SendMessage,
    SendMessageBuilder,
    SendMessageTester,
  };
});
