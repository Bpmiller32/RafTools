import { defineStore } from "pinia";
import { ref } from "vue";

export const useStore = defineStore("main", () => {
  // Websocket connections
  const connectionCrawler = ref(null);
  const connectionBuilder = ref(null);
  const connectionTester = ref(null);

  // Service stores
  const crawlers = ref({
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
  });
  const builders = ref({
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
  });
  const testers = ref({
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
  });

  function SendMessage(Directory, Property, Value) {
    connectionCrawler.value.send(
      JSON.stringify({ Directory, Property, Value })
    );
  }
  function SendMessageBuilder(Directory, Property, Value) {
    connectionBuilder.value.send(
      JSON.stringify({ Directory, Property, Value })
    );
  }
  function SendMessageTester(Directory, Property, Value) {
    connectionTester.value.send(JSON.stringify({ Directory, Property, Value }));
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
