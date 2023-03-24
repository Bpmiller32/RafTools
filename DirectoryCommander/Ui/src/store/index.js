import { defineStore } from "pinia";

export const useStore = defineStore("main", () => {
  // Websocket connections
  let connectionCrawler = null;
  let connectionBuilder = null;
  let connectionTester = null;

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

  function SendMessageCrawler(Directory, Action, Data01) {
    this.connectionCrawler.send(JSON.stringify({ Directory, Action, Data01 }));
  }
  function SendMessageBuilder(Directory, Action, Data01, Data02) {
    this.connectionBuilder.send(
      JSON.stringify({ Directory, Action, Data01, Data02 })
    );
  }
  function SendMessageTester(Directory, Action, Data01) {
    this.connectionTester.send(JSON.stringify({ Directory, Action, Data01 }));
  }

  return {
    connectionCrawler,
    connectionBuilder,
    connectionTester,
    crawlers,
    builders,
    testers,
    SendMessageCrawler,
    SendMessageBuilder,
    SendMessageTester,
  };
});
