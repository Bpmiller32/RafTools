import {
  PropType,
  TransitionGroup,
  defineComponent,
  onMounted,
  ref,
} from "vue";
import BackEndModule from "../interfaces/BackEndModule";
import anime from "animejs/lib/anime.es.js";
import {
  StatusOnlineIcon,
  ArrowCircleDownIcon,
  ExclamationCircleIcon,
  RefreshIcon,
  SelectorIcon,
  CheckIcon,
} from "@heroicons/vue/outline";
import ParascriptLogo from "../assets/ParascriptLogo.png";
import {
  Listbox,
  ListboxButton,
  ListboxLabel,
  ListboxOption,
  ListboxOptions,
} from "@headlessui/vue";
import ListDirectory from "../interfaces/ListDirectory";
import ListboxOptionProperties from "../interfaces/ListboxOptionProperties";

export default defineComponent({
  props: {
    crawlermodule: Object as PropType<BackEndModule>,
    buildermodule: Object as PropType<BackEndModule>,
  },
  setup(props) {
    const people = [
      { id: 1, name: "Durward Reynolds" },
      { id: 2, name: "Kenton Towne" },
      { id: 3, name: "Therese Wunsch" },
      { id: 4, name: "Benedict Kessler" },
      { id: 5, name: "Katelyn Rohan" },
      { id: 6, name: "Billy Miller" },
    ];
    // const selectedPerson = ref(null);
    const selectedPerson = ref(people[0]);

    /* -------------------------------------------------------------------------- */
    /*                            DirectoryState object                           */
    /* -------------------------------------------------------------------------- */
    const directoriesState = ref({
      directories: [] as ListDirectory[],
      selectedDirectory: {} as ListDirectory,
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
      FormatData: () => {
        directoriesState.value.directories = [];

        const readyDataYearMonths =
          props.crawlermodule?.ReadyToBuild.DataYearMonth.split("|").reverse();

        // Format directories into objects
        readyDataYearMonths?.forEach((directory, index) => {
          const monthNum = directory.substring(4, 6);
          const yearNum = directory.substring(0, 4);

          const dir: ListDirectory = {
            name:
              directoriesState.value.monthNames.get(monthNum) + " " + yearNum,
            icon: undefined,
            fileCount: "",
            downloadDate: "",
            downloadTime: "",
            isNew: false,
          };
          directoriesState.value.directories.push(dir);
        });
      },
    });

    /* -------------------------------------------------------------------------- */
    /*                         Mounting and watchers setup                        */
    /* -------------------------------------------------------------------------- */
    onMounted(() => {
      directoriesState.value.FormatData();
    });

    /* -------------------------------------------------------------------------- */
    /*                                Subcomponents                               */
    /* -------------------------------------------------------------------------- */
    function StatusLabel() {
      return (
        <TransitionGroup
          enterFromClass="opacity-0 translate-y-[-0.25rem]"
          enterToClass="opacity-100"
          enterActiveClass="duration-[300ms]"
        >
          {() => {
            switch (props.buildermodule?.Status) {
              case 0:
                return (
                  <div
                    key="0"
                    class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-green-800 bg-green-100"
                  >
                    Ready
                  </div>
                );

              case 1:
                return (
                  <div
                    key="1"
                    class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-yellow-800 bg-yellow-100"
                  >
                    In Progress
                  </div>
                );

              default:
                return (
                  <div
                    key="default"
                    class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-red-800 bg-red-100"
                  >
                    Error
                  </div>
                );
            }
          }}
        </TransitionGroup>
      );
    }

    function StatusIcon() {
      return (
        <TransitionGroup
          enterFromClass="opacity-0 translate-y-[-0.5rem]"
          enterToClass="opacity-100"
          enterActiveClass="duration-[500ms]"
        >
          {() => {
            switch (props.buildermodule?.Status) {
              case 0:
                return (
                  <StatusOnlineIcon
                    key="0"
                    class="h-5 w-5 ml-1 text-green-500"
                  />
                );

              case 1:
                return (
                  <ArrowCircleDownIcon
                    key="1"
                    class="h-5 w-5 ml-1 text-yellow-500"
                  />
                );

              default:
                return (
                  <ExclamationCircleIcon
                    key="default"
                    class="h-5 w-5 ml-1 text-red-500"
                  />
                );
            }
          }}
        </TransitionGroup>
      );
    }

    function ListBoxButton() {
      return (
        <ListboxButton
          class={{
            "cursor-not-allowed": props.buildermodule?.Status != 0,
            "relative w-full bg-white border border-gray-300 rounded-md shadow-sm pl-3 pr-10 py-2 cursor-default focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500":
              true,
          }}
        >
          {ListBoxButtonHelper()}
          <span class="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none">
            <SelectorIcon class="h-5 w-5 text-gray-400" />
          </span>
        </ListboxButton>
      );
    }

    function ListBoxButtonHelper() {
      if (selectedPerson.value == null) {
        return (
          <div class="flex items-center">
            <div class="ml-2">No directories available</div>
          </div>
        );
      } else {
        return (
          <div class="flex items-center">
            <div
              class={{
                "bg-green-400": true,
                "bg-yellow-400": false,
                "bg-gray-200": false,
                "h-2 w-2 rounded-full": true,
              }}
            />
            <div class="ml-3">{selectedPerson.value.name}</div>
          </div>
        );
      }
    }

    function ListBoxOptions() {
      return (
        <ListboxOptions class="absolute z-20 mt-1 w-full bg-white shadow-lg max-h-[15rem] rounded-md py-1 text-base ring-1 ring-black ring-opacity-5 overflow-auto focus:outline-none">
          {people.map((person) => (
            <ListboxOption key={person.id} value={person}>
              {(uiOptions: ListboxOptionProperties) => (
                <li
                  class={{
                    "text-white bg-indigo-600": uiOptions.active == true,
                    "text-gray-900": uiOptions.active == false,
                    "cursor-default relative py-2 pl-3 pr-9": true,
                  }}
                >
                  <div class="flex items-center">
                    <span
                      class={{
                        "bg-green-400": true,
                        "bg-yellow-400": false,
                        "bg-gray-200": false,
                        "flex-shrink-0 h-2 w-2 rounded-full": true,
                      }}
                    />
                    <span
                      class={{
                        "font-semibold": uiOptions.selected == true,
                        "font-normal": uiOptions.selected == false,
                        "ml-3 truncate": true,
                      }}
                    >
                      {person.name}
                    </span>
                  </div>

                  <span
                    class={{
                      "text-white": uiOptions.active == true,
                      "text-indigo-600": uiOptions.active == false,
                      "absolute inset-y-0 right-0 flex items-center pr-4": true,
                    }}
                  >
                    {uiOptions.selected ? (
                      <CheckIcon class="h-5 w-5" />
                    ) : (
                      <div></div>
                    )}
                  </span>
                </li>
              )}
            </ListboxOption>
          ))}
        </ListboxOptions>
      );
    }

    function BuildButton() {
      return (
        <button
          //   ref={downloadButtonRef}
          //   onClick={CrawlButtonClicked}
          type="button"
          disabled={props.buildermodule?.Status == 0 ? false : true}
          class={{
            "cursor-not-allowed ": props.buildermodule?.Status != 0,
            "my-6 flex items-center px-2 py-2 max-h-8 bg-gradient-to-r bg-gray-500 from-indigo-600 to-indigo-600 hover:from-indigo-700 hover:to-indigo-700 bg-no-repeat bg-center border border-transparent text-sm text-white leading-4 font-medium rounded-md focus:outline-none":
              true,
          }}
        >
          <RefreshIcon
            // ref={refreshIconRef}
            class="shrink-0 h-5 w-5 text-white z-10"
          />
          <TransitionGroup
            enterFromClass="opacity-0"
            enterToClass="opacity-100"
            enterActiveClass="duration-[1500ms]"
          >
            {() => {
              switch (props.buildermodule?.Status) {
                case 0:
                  return (
                    <p key="0" class="ml-1">
                      Build Directory
                    </p>
                  );

                default:
                  return (
                    <p key="default" class="ml-1">
                      Building ....
                    </p>
                  );
              }
            }}
          </TransitionGroup>
        </button>
      );
    }

    function ProgressSlideDown() {
      return (
        <div class="overflow-hidden h-14">
          {" "}
          <div class="flex justify-center text-sm font-medium text-gray-700">
            Currently Building:
          </div>
          <div class="min-w-[16rem] mt-1 mb-4 bg-gray-200 rounded-full dark:bg-gray-700">
            <div
              class="bg-indigo-600 text-xs font-medium text-indigo-100 text-center p-0.5 leading-none rounded-full"
              style="
                  'width: ' + store.builders[props.dirType].Progress + '%'
                "
            >
              99%
            </div>
          </div>
        </div>
      );
    }

    /* -------------------------------------------------------------------------- */
    /*                               Render function                              */
    /* -------------------------------------------------------------------------- */
    return () => (
      <div class="select-none min-w-[18rem] max-w-[18rem] bg-white rounded-lg shadow divide-y divide-gray-200">
        <div class="p-6">
          <div class="flex justify-center">
            <img class="w-20 h-20 border rounded-full" src={ParascriptLogo} />
          </div>

          <div class="flex justify-center mt-4 items-center shrink-0">
            <p class="text-gray-900 text-sm font-medium">Parascript</p>
            {StatusLabel()}
            {StatusIcon()}
          </div>

          <div class="mt-6">
            <Listbox as="div" v-model={selectedPerson.value}>
              <ListboxLabel class="mt-2 text-sm font-medium text-gray-900">
                Select Month to Build
              </ListboxLabel>

              <div class="mt-1 relative">
                {ListBoxButton()}
                {ListBoxOptions()}
              </div>
            </Listbox>
          </div>
        </div>
        <div class="flex justify-center">
          <div>
            <div class="flex justify-center">{BuildButton()}</div>
            {ProgressSlideDown()}
          </div>
        </div>
      </div>
    );
  },
});
