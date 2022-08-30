import { defineStore } from "pinia";
import { ref } from "vue";

export const useStore = defineStore("main", () => {
  const connectionCrawler = ref(null);
  const connectionBuilder = ref(null);

  const crawlers = ref({
    SmartMatch: {
      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,

      AvailableBuilds: [],
    },
    Parascript: {
      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,

      AvailableBuilds: [],
    },
    RoyalMail: {
      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,

      AvailableBuilds: [],
    },
  });
  const builders = ref({
    SmartMatch: {
      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,

      CompiledBuilds: [],
      CurrentBuild: null,
      Progress: null,
    },
    Parascript: {
      DirectoryStatus: null,
      AutoEnabled: null,
      AutoDate: null,

      CompiledBuilds: [],
      CurrentBuild: null,
      Progress: null,
    },
    RoyalMail: {
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
      DirectoryStatus: null,
      CurrentBuild: null,
      Progress: null,
    },
    SmartMatchZip4: {
      DirectoryStatus: null,
      CurrentBuild: null,
      Progress: null,
    },
    Parascript: {
      DirectoryStatus: null,
      CurrentBuild: null,
      Progress: null,
    },
    RoyalMail3: {
      DirectoryStatus: null,
      CurrentBuild: null,
      Progress: null,
    },
    RoyalMailWin7: {
      DirectoryStatus: null,
      CurrentBuild: null,
      Progress: null,
    },
    RoyalMailXP: {
      DirectoryStatus: null,
      CurrentBuild: null,
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

  return {
    connectionCrawler,
    connectionBuilder,
    crawlers,
    builders,
    testers,
    SendMessage,
    SendMessageBuilder,
  };
});
