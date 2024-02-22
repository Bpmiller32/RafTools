import { defineComponent, ref } from "vue";
import ErrorLogo from "../assets/ErrorLogo.png";
import SmartMatchLogo from "../assets/usa.png";
import ParascriptLogo from "../assets/hw.png";
import RoyalMailLogo from "../assets/uk.png";

export default defineComponent({
  props: {
    name: String,
    directories: String,
  },
  setup(props) {
    const directoriesState = ref({
      directories: [],
      monthNames: new Map([
        ["01", "January"],
        ["02", "February"],
        ["03", "March"],
        ["04", "April"],
        ["05", "May"],
        ["06", "June"],
        ["07", "July"],
        ["08", "August"],
        ["09", "September"],
        ["10", "October"],
        ["11", "November"],
        ["12", "December"],
      ]),
      icons: new Map([
        ["01", new URL("../assets/january.png", import.meta.url).href],
        ["02", new URL("../assets/february.png", import.meta.url).href],
        ["03", new URL("../assets/march.png", import.meta.url).href],
        ["04", new URL("../assets/april.png", import.meta.url).href],
        ["05", new URL("../assets/may.png", import.meta.url).href],
        ["06", new URL("../assets/june.png", import.meta.url).href],
        ["07", new URL("../assets/july.png", import.meta.url).href],
        ["08", new URL("../assets/august.png", import.meta.url).href],
        ["09", new URL("../assets/september.png", import.meta.url).href],
        ["10", new URL("../assets/october.png", import.meta.url).href],
        ["11", new URL("../assets/november.png", import.meta.url).href],
        ["12", new URL("../assets/december.png", import.meta.url).href],
      ]),
      FormatData: () => {
        directoriesState.value.directories = [];
        const today = new Date();
        const thisMonth = new Date(today.getFullYear(), today.getMonth(), 1);

        const separatedDirectories = props.directories?.split("|");

        separatedDirectories?.forEach((directory, index) => {
          const monthNum = directory.substring(4, 6);
          const yearNum = directory.substring(0, 4);
          let isNew = false;

          const name =
            directoriesState.value.monthNames.get(monthNum) + " " + yearNum;
          const icon = directoriesState.value.icons.get(monthNum);
        });

        // for (
        //   let index = 0;
        //   index < store.crawlers[props.dirType].AvailableBuilds.length;
        //   index++
        // ) {
        //   const monthNum = store.crawlers[props.dirType].AvailableBuilds[
        //     index
        //   ].Name.substring(4, 6);
        //   const yearNum = store.crawlers[props.dirType].AvailableBuilds[
        //     index
        //   ].Name.substring(0, 4);
        //   let isNew = false;

        //   const name =
        //     directoriesState.value.monthNames.get(monthNum) + " " + yearNum;
        //   const icon = directoriesState.value.icons.get(monthNum);

        //   const dateString =
        //     store.crawlers[props.dirType].AvailableBuilds[index].Date.split(
        //       "/"
        //     );
        //   const dirDate = new Date(
        //     dateString[2],
        //     dateString[0] - 1,
        //     parseInt(dateString[1])
        //   );

        //   if (dirDate >= thisMonth) {
        //     isNew = true;
        //   }

        //   const dir = {
        //     name: name,
        //     fileCount:
        //       store.crawlers[props.dirType].AvailableBuilds[index].FileCount,
        //     date: store.crawlers[props.dirType].AvailableBuilds[index].Date,
        //     time: store.crawlers[props.dirType].AvailableBuilds[index].Time,
        //     icon: icon,
        //     isNew: isNew,
        //   };

        //   directoriesState.value.directories.push(dir);
        // }
      },
    });

    function DirectoryImage() {
      switch (props.name) {
        case "SmartMatch":
          return <img class="w-10 h-10" src={SmartMatchLogo} />;
        case "Parascript":
          return <img class="w-10 h-10" src={ParascriptLogo} />;
        case "RoyalMail":
          return <img class="w-10 h-10" src={RoyalMailLogo} />;

        default:
          return <img class="w-10 h-10" src={ErrorLogo} />;
      }
    }

    function DirectoryList() {}

    // Render function
    return () => (
      <div class="select-none bg-white pb-4 rounded-lg shadow max-w-sm min-w-[10rem]">
        <div class="flex justify-between items-center px-6 py-4 border-b-[1px] border-gray-400">
          <div class="text-gray-900 text-sm font-medium">
            Directories Downloaded
          </div>
          {DirectoryImage()}
        </div>
        <ul class="mx-4 overflow-y-scroll max-h-40 max-w-sm divide-y divide-gray-400">
          {DirectoryList()}
        </ul>
      </div>
    );
  },
});
