import {
  PropType,
  TransitionGroup,
  defineComponent,
  onMounted,
  ref,
  watch,
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
  XCircleIcon,
  CheckCircleIcon,
} from "@heroicons/vue/outline";
import SmartMatchLogo from "../assets/SmartMatchLogo.png";
import {
  Listbox,
  ListboxButton,
  ListboxLabel,
  ListboxOption,
  ListboxOptions,
  RadioGroup,
  RadioGroupDescription,
  RadioGroupLabel,
  RadioGroupOption,
} from "@headlessui/vue";
import ListDirectory from "../interfaces/ListDirectory";
import ListboxOptionProperties from "../interfaces/ListboxOptionProperties";
import RadioOptionProperties from "../interfaces/RadioOptionProperties";
import { useGlobalState } from "../store";

export default defineComponent({
  props: {
    crawlermodule: Object as PropType<BackEndModule>,
    buildermodule: Object as PropType<BackEndModule>,
  },
  setup(props) {
    /* -------------------------------------------------------------------------- */
    /*                              CassCycles array                              */
    /* -------------------------------------------------------------------------- */
    const cassCycles = [
      {
        name: "Cycle O",
        description: "Build with CASS Cycle O in standard mode",
      },
      {
        name: "Cycle N",
        description: "Build CASS Cycle N using CASS Cycle O data",
      },
      {
        name: "MASS O",
        description: "Build CASS Cycle O using MASS data",
      },
    ];

    /* -------------------------------------------------------------------------- */
    /*                                    State                                   */
    /* -------------------------------------------------------------------------- */
    const state = useGlobalState();
    const selectedDirectory = ref();
    const selectedCassCycle = ref(cassCycles[0]);
    const expirationDate = ref();
    const directoriesAvailable = ref();

    /* -------------------------------------------------------------------------- */
    /*                            DirectoryState object                           */
    /* -------------------------------------------------------------------------- */
    const directoriesState = ref({
      directories: [] as ListDirectory[],
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
        const builtDataYearMonths =
          props.buildermodule?.BuildComplete.DataYearMonth.split("|");

        // Format directories into objects
        readyDataYearMonths?.forEach((directory) => {
          const monthNum = directory.substring(4, 6);
          const yearNum = directory.substring(0, 4);

          let isBuilt = false;
          if (builtDataYearMonths?.includes(directory)) {
            isBuilt = true;
          }

          const dir: ListDirectory = {
            name:
              directoriesState.value.monthNames.get(monthNum) + " " + yearNum,
            fullName: directory,
            icon: undefined,
            fileCount: "",
            downloadDate: "",
            downloadTime: "",
            isNew: false,
            isBuilt: isBuilt,
          };
          directoriesState.value.directories.push(dir);
        });
      },
    });

    /* -------------------------------------------------------------------------- */
    /*                            Animation refs setup                            */
    /* -------------------------------------------------------------------------- */
    const refreshIconRef = ref();
    let refreshIconAnimation: anime.AnimeInstance;

    const downloadButtonRef = ref();
    let downloadButtonFillAnimation: anime.AnimeInstance;
    let downloadButtonDrainAnimation: anime.AnimeInstance;

    const cancelButtonRef = ref();
    let cancelButtonEnterAnimation: anime.AnimeInstance;
    let cancelButtonLeaveAnimation: anime.AnimeInstance;

    const progressSlideDownRef = ref();
    let progressSlideDownEnterAnimation: anime.AnimeInstance;
    let progressSlideDownLeaveAnimation: anime.AnimeInstance;

    /* -------------------------------------------------------------------------- */
    /*                         Mounting and watchers setup                        */
    /* -------------------------------------------------------------------------- */
    onMounted(() => {
      // Animation
      refreshIconAnimation = anime({
        targets: refreshIconRef.value,
        rotate: "-=2turn",
        easing: "easeInOutSine",
        loop: true,
        autoplay: false,
      });

      downloadButtonFillAnimation = anime({
        targets: downloadButtonRef.value,
        duration: 300,
        backgroundSize: ["0% 0%", "150% 150%"],
        width: ["7.5rem", "8.75rem"],
        marginLeft: ["4.5rem", "3.75rem"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      downloadButtonDrainAnimation = anime({
        targets: downloadButtonRef.value,
        duration: 300,
        backgroundSize: ["150% 150%", "0% 0%"],
        width: ["8.75rem", "7.5rem"],
        marginLeft: ["3.75rem", "4.5rem"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      cancelButtonEnterAnimation = anime({
        targets: cancelButtonRef.value,
        duration: 500,
        translateY: ["0.5rem", "0rem"],
        opacity: ["0", "0.9999"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      cancelButtonLeaveAnimation = anime({
        targets: cancelButtonRef.value,
        duration: 500,
        translateY: ["0rem", "0.5rem"],
        opacity: ["0.9999", "0"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      progressSlideDownEnterAnimation = anime({
        targets: progressSlideDownRef.value,
        duration: 500,
        height: ["0rem", "5.5rem"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      progressSlideDownLeaveAnimation = anime({
        targets: progressSlideDownRef.value,
        duration: 500,
        height: ["5.5rem", "0rem"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      // Listbox and directorystate init
      directoriesState.value.FormatData();
      selectedDirectory.value = directoriesState.value.directories[0];

      if (selectedDirectory.value.name == "undefined ") {
        directoriesAvailable.value = false;
      } else {
        directoriesAvailable.value = true;
      }

      directoriesState.value.directories.forEach((directory) => {
        if (directory.fullName == props.buildermodule?.CurrentTask) {
          selectedDirectory.value = directory;
        }
      });

      // First draw/mount tweaks
      switch (props.buildermodule?.Status) {
        case 1:
          refreshIconAnimation.play();
          downloadButtonRef.value.style.width = "7.5rem";
          downloadButtonRef.value.style.marginLeft = "4.5rem";
          downloadButtonRef.value.style.backgroundSize = "0% 0%";
          break;
        case 2:
          downloadButtonDrainAnimation.play();
          refreshIconAnimation.pause();
          cancelButtonRef.value.style.opacity = "0";
          break;

        default:
          refreshIconAnimation.pause();
          cancelButtonRef.value.style.opacity = "0";
          progressSlideDownRef.value.style.height = "0rem";
          break;
      }
    });

    // Watch if new directories are added to be built
    watch(
      () => props.crawlermodule?.ReadyToBuild.DataYearMonth,
      () => {
        directoriesState.value.FormatData();

        if (directoriesState.value.directories[0].name != "undefined ") {
          directoriesAvailable.value = true;
        }
      }
    );

    // Watch if status of the module changes
    watch(
      () => props.buildermodule?.Status,
      () => {
        switch (props.buildermodule?.Status) {
          case 1:
            refreshIconAnimation.play();
            downloadButtonDrainAnimation.play();
            cancelButtonEnterAnimation.play();
            progressSlideDownEnterAnimation.play();
            break;
          case 2:
            refreshIconAnimation.pause();
            downloadButtonDrainAnimation.play();
            cancelButtonLeaveAnimation.play();
            progressSlideDownEnterAnimation.play();
            break;

          default:
            refreshIconAnimation.pause();
            downloadButtonFillAnimation.play();
            cancelButtonLeaveAnimation.play();
            progressSlideDownLeaveAnimation.play();
            break;
        }
      }
    );

    /* -------------------------------------------------------------------------- */
    /*                                   Events                                   */
    /* -------------------------------------------------------------------------- */
    function BuildButtonClicked() {
      // Do nothing if Builder is not in the ready state
      if (
        props.buildermodule?.Status != 0 ||
        !selectedDirectory.value.fullName
      ) {
        return;
      }

      let expireDays;
      if (expirationDate.value == undefined) {
        expireDays = "";
      } else {
        /// Convert givenDate and today's date to milliseconds
        const expiration = new Date(expirationDate.value);
        const expirationTime = expiration.getTime();
        const currentTime = new Date().getTime();

        // Calculate the difference in milliseconds
        const difference = Math.abs(currentTime - expirationTime);

        // Convert milliseconds to days
        expireDays = Math.ceil(difference / (1000 * 60 * 60 * 24));
      }

      let cycle;
      switch (selectedCassCycle.value.name) {
        case "Cycle N":
          cycle = "OtoN";
          break;
        case "MASS O":
          cycle = "MASSO";
          break;

        default:
          cycle = "O";
          break;
      }

      // PROD
      // Define the request options
      const requestOptions = {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          moduleCommand: "start",
          dataYearMonth: selectedDirectory.value.fullName,
          cycle: cycle,
          expireDays: expireDays,
        }),
      };

      // Send the request using the Fetch API
      fetch(state.beUrl.value + "/smartmatch/builder", requestOptions).then(
        (response) => {
          if (!response.ok) {
            throw new Error("Network response was not ok");
          }
        }
      );
    }

    function CancelButtonClicked() {
      // Do nothing if Builder is not in the in progress state
      if (props.buildermodule?.Status != 1) {
        return;
      }

      const requestOptions = {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          moduleCommand: "stop",
        }),
      };

      // Send the request using the Fetch API
      fetch(state.beUrl.value + "/smartmatch/builder", requestOptions).then(
        (response) => {
          if (!response.ok) {
            throw new Error("Network response was not ok");
          }
        }
      );
    }

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
          // @ts-ignore
          disabled={
            props.buildermodule?.Status != 0 || !directoriesAvailable.value
          }
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
      if (!directoriesAvailable.value) {
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
                "bg-green-400":
                  selectedDirectory.value.isBuilt == true &&
                  props.buildermodule?.Status != 1,
                "bg-yellow-400":
                  props.buildermodule?.Status == 1 ||
                  props.buildermodule?.Status == 2,
                "bg-gray-200":
                  selectedDirectory.value.isBuilt == false &&
                  props.buildermodule?.Status != 1,
                "h-2 w-2 rounded-full": true,
              }}
            />
            <div class="ml-3">{selectedDirectory.value.name}</div>
          </div>
        );
      }
    }

    function ListBoxOptions() {
      return (
        <ListboxOptions class="absolute z-20 mt-1 w-full bg-white shadow-lg max-h-[15rem] rounded-md py-1 text-base ring-1 ring-black ring-opacity-5 overflow-auto focus:outline-none">
          {directoriesState.value.directories.map((directory, index) => (
            <ListboxOption key={index} value={directory}>
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
                        "bg-green-400": directory.isBuilt == true,
                        "bg-yellow-400": false,
                        "bg-gray-200": directory.isBuilt == false,
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
                      {directory.name}
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
          ref={downloadButtonRef}
          onClick={BuildButtonClicked}
          type="button"
          disabled={props.buildermodule?.Status == 0 ? false : true}
          class={{
            "cursor-not-allowed ": props.buildermodule?.Status != 0,
            "col-span-5 ml-14 my-6 flex items-center px-2 py-2 max-h-8 bg-gradient-to-r bg-gray-500 from-indigo-600 to-indigo-600 hover:from-indigo-700 hover:to-indigo-700 bg-no-repeat bg-center border border-transparent text-sm text-white leading-4 font-medium rounded-md focus:outline-none":
              true,
          }}
        >
          <RefreshIcon
            ref={refreshIconRef}
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
                    <p key="0" class="ml-1 shrink-0">
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

    function CancelButton() {
      return (
        <XCircleIcon
          ref={cancelButtonRef}
          onClick={CancelButtonClicked}
          class={{
            "select-none": props.buildermodule?.Status == 0,
            "cursor-pointer": props.buildermodule?.Status == 1,
            " h-6 w-6 text-red-500": true,
          }}
        />
      );
    }

    function ProgressSlideDown() {
      return (
        <div ref={progressSlideDownRef} class="overflow-hidden h-14">
          {ProgressSlideDownHelper()}
          <div class="min-w-[16rem] mt-1 mb-4 bg-gray-200 rounded-full dark:bg-gray-700">
            <div
              class="bg-indigo-600 text-xs font-medium text-indigo-100 text-center p-0.5 leading-none rounded-full"
              style={{ width: props.buildermodule?.Progress + "%" }}
            >
              {props.buildermodule?.Progress}%
            </div>
          </div>
          <div class="flex justify-center text-sm text-gray-500">
            Task:{" "}
            {props.buildermodule?.Message != ""
              ? props.buildermodule?.Message
              : "Not available"}
          </div>
        </div>
      );
    }

    function ProgressSlideDownHelper() {
      if (selectedDirectory.value) {
        return (
          <div class="flex justify-center text-sm font-medium text-gray-700">
            Currently Building: {selectedDirectory.value.name}
          </div>
        );
      }
    }

    function CycleSelectRadio() {
      return (
        <RadioGroup
          //@ts-ignore
          disabled={
            props.buildermodule?.Status != 0 || !directoriesAvailable.value
          }
          class="mt-6"
          v-model={selectedCassCycle.value}
        >
          <RadioGroupLabel class="mt-2 text-sm font-medium text-gray-900">
            Select CASS Cycle
          </RadioGroupLabel>

          <div class="mt-1 grid grid-cols-1 grid-rows-3 gap-y-2">
            {cassCycles.map((cassCycle, index) => (
              <RadioGroupOption key={index} value={cassCycle}>
                {(uiOptions: RadioOptionProperties) => (
                  <div
                    class={{
                      "border-indigo-600 ring-2 ring-indigo-600":
                        uiOptions.active == true,
                      "border-gray-300": uiOptions.active == false,
                      "relative flex rounded-lg border bg-white p-4 shadow-sm focus:outline-none":
                        true,
                    }}
                  >
                    <span class="flex flex-1">
                      <span class="flex flex-col">
                        <RadioGroupLabel
                          as="span"
                          class="block text-sm font-medium text-gray-900"
                        >
                          {cassCycle.name}
                        </RadioGroupLabel>
                        <RadioGroupDescription
                          as="span"
                          class="mt-1 flex items-center text-sm text-gray-500"
                        >
                          {cassCycle.description}
                        </RadioGroupDescription>
                      </span>
                    </span>
                    <CheckCircleIcon
                      class={{
                        "h-5 w-5 text-indigo-600": uiOptions.checked == true,
                        "h-5 w-5 opacity-0": uiOptions.checked == false,
                      }}
                    />
                  </div>
                )}
              </RadioGroupOption>
            ))}
          </div>
        </RadioGroup>
      );
    }

    function ExpirationDateInput() {
      return (
        <div class="mt-6">
          <div class="mt-2 text-sm font-medium text-gray-900">
            Enter Custom Expiration Date
          </div>

          <input
            //@ts-ignore
            disabled={
              props.buildermodule?.Status != 0 || !directoriesAvailable.value
            }
            v-model={expirationDate.value}
            type="date"
            class="mt-2 w-full h-10 text-xs text-center rounded-md border-0 py-1.5 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-indigo-600"
            placeholder="mm/dd/yyyy"
          />
        </div>
      );
    }

    /* -------------------------------------------------------------------------- */
    /*                               Render function                              */
    /* -------------------------------------------------------------------------- */
    return () => (
      <div class="select-none min-w-[18rem] max-w-[18rem] h-fit bg-white rounded-lg shadow divide-y divide-gray-200">
        <div class="p-6">
          <div class="flex justify-center">
            <img class="w-20 h-20 border rounded-full" src={SmartMatchLogo} />
          </div>

          <div class="flex mt-4 items-center shrink-0">
            <p class="text-gray-900 text-sm font-medium ml-10 py-2">
              SmartMatch
            </p>
            {StatusLabel()}
            {StatusIcon()}
          </div>

          <div class="mt-6">
            <Listbox as="div" v-model={selectedDirectory.value}>
              <ListboxLabel class="mt-2 text-sm font-medium text-gray-900">
                Select Month to Build
              </ListboxLabel>

              <div class="mt-1 relative">
                {ListBoxButton()}
                {ListBoxOptions()}
              </div>
            </Listbox>
          </div>

          {CycleSelectRadio()}
          {ExpirationDateInput()}
        </div>
        <div class="flex justify-center">
          <div>
            <div class="grid grid-cols-6 grid-rows-1 items-center">
              {BuildButton()}
              {CancelButton()}
            </div>
            {ProgressSlideDown()}
          </div>
        </div>
      </div>
    );
  },
});
