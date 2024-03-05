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
  CheckCircleIcon,
  XCircleIcon,
} from "@heroicons/vue/outline";

import TowerBlank from "../assets/towerBlank.png";
import Tower1 from "../assets/tower1.png";
import Tower2 from "../assets/tower2.png";
import Tower3 from "../assets/tower3.png";
import { useGlobalState } from "../store";
import {
  RadioGroup,
  RadioGroupDescription,
  RadioGroupLabel,
  RadioGroupOption,
} from "@headlessui/vue";
import RadioOptionProperties from "../interfaces/RadioOptionProperties";

export default defineComponent({
  props: {
    module: Object as PropType<BackEndModule>,
  },
  setup(props) {
    const testOperations = [
      {
        name: "SmartMatch",
        description: "Place DVD in Tray 1",
      },
      {
        name: "Parascript",
        description: "Place DVD in Tray 2",
      },
      {
        name: "RoyalMail",
        description: "Place DVD in Tray 3",
      },
    ];

    const selectedTestOperation = ref(testOperations[0]);

    /* -------------------------------------------------------------------------- */
    /*                                Global state                                */
    /* -------------------------------------------------------------------------- */
    const state = useGlobalState();

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
        width: ["6rem", "4.75rem"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      downloadButtonDrainAnimation = anime({
        targets: downloadButtonRef.value,
        duration: 300,
        backgroundSize: ["150% 150%", "0% 0%"],
        width: ["4.75rem", "6rem"],
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

      // First draw/mount tweaks
      switch (props.module?.Status) {
        case 1:
          refreshIconAnimation.play();
          downloadButtonRef.value.style.width = "6rem";
          downloadButtonRef.value.style.backgroundSize = "0% 0%";
          cancelButtonRef.value.style.opacity = "0";
          break;
        case 2:
          downloadButtonDrainAnimation.play();
          refreshIconAnimation.pause();
          break;

        default:
          refreshIconAnimation.pause();
          progressSlideDownRef.value.style.height = "0rem";
          cancelButtonRef.value.style.opacity = "0";
          break;
      }
    });

    // Watch if status of the module changes
    watch(
      () => props.module?.Status,
      () => {
        switch (props.module?.Status) {
          case 1:
            refreshIconAnimation.play();
            downloadButtonDrainAnimation.play();
            cancelButtonLeaveAnimation.play();
            progressSlideDownEnterAnimation.play();
            break;
          case 2:
            downloadButtonDrainAnimation.play();
            cancelButtonEnterAnimation.play();
            refreshIconAnimation.pause();
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
    function CrawlButtonClicked() {
      // PROD
      // Define the request options
      const requestOptions = {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          moduleCommand: "start",
        }),
      };

      // Send the request using the Fetch API
      fetch(state.beUrl.value + "/dirTester", requestOptions).then(
        (response) => {
          if (!response.ok) {
            throw new Error("Network response was not ok");
          }
        }
      );
    }

    function CancelButtonClicked() {
      // Do nothing if Builder is not in the in progress state
      if (props.module?.Status != 1) {
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
      fetch(state.beUrl.value + "/dirtester", requestOptions).then(
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
            switch (props.module?.Status) {
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
            switch (props.module?.Status) {
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

    function DirectoryImage() {
      switch (selectedTestOperation.value.name) {
        case "SmartMatch":
          return (
            <img
              class="justify-self-end w-20 h-20 border rounded-full"
              src={Tower1}
            />
          );
        case "Parascript":
          return (
            <img
              class="justify-self-end w-20 h-20 border rounded-full"
              src={Tower2}
            />
          );
        case "RoyalMail":
          return (
            <img
              class="justify-self-end w-20 h-20 border rounded-full"
              src={Tower3}
            />
          );

        default:
          return (
            <img
              class="justify-self-end w-20 h-20 border rounded-full"
              src={TowerBlank}
            />
          );
      }
    }

    function DownloadButton() {
      return (
        <button
          ref={downloadButtonRef}
          onClick={CrawlButtonClicked}
          type="button"
          disabled={props.module?.Status == 0 ? false : true}
          class={{
            "cursor-not-allowed ": props.module?.Status != 0,
            "justify-self-center flex items-center px-2 py-2 max-h-8 bg-gradient-to-r bg-gray-500 from-indigo-600 to-indigo-600 hover:from-indigo-700 hover:to-indigo-700 bg-no-repeat bg-center border border-transparent text-sm text-white leading-4 font-medium rounded-md focus:outline-none":
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
              switch (props.module?.Status) {
                case 0:
                  return (
                    <p key="0" class="ml-1">
                      Test
                    </p>
                  );

                default:
                  return (
                    <p key="default" class="ml-1">
                      Testing
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
            "select-none": props.module?.Status == 0,
            "cursor-pointer": props.module?.Status == 1,
            " h-6 w-6 text-red-500": true,
          }}
        />
      );
    }

    function ProgressSlideDown() {
      return (
        <div ref={progressSlideDownRef} class="overflow-hidden h-14">
          {ProgressSlideDownHelper()}
          <div class="flex justify-center">
            <div class="min-w-[20rem] max-w-[20rem] mt-1 mb-4 bg-gray-200 rounded-full dark:bg-gray-700">
              <div
                class="bg-indigo-600 text-xs font-medium text-indigo-100 text-center p-0.5 leading-none rounded-full"
                style={{ width: props.module?.Progress + "%" }}
              >
                {props.module?.Progress}%
              </div>
            </div>
          </div>
          <div class="flex justify-center text-sm text-gray-500">
            Task:{" "}
            {props.module?.Message != ""
              ? props.module?.Message
              : "Not available"}
          </div>
        </div>
      );
    }

    function ProgressSlideDownHelper() {
      if (selectedTestOperation.value) {
        return (
          <div class="flex justify-center text-sm font-medium text-gray-700">
            Currently Testing: {selectedTestOperation.value.name}
          </div>
        );
      }
    }

    function OperationSelectRadio() {
      return (
        <RadioGroup
          //@ts-ignore
          // disabled={
          //   props.buildermodule?.Status != 0 || !directoriesAvailable.value
          // }
          class="mt-6 col-span-3"
          v-model={selectedTestOperation.value}
        >
          <RadioGroupLabel class="mt-2 text-sm font-medium text-gray-900">
            Select Test Operation
          </RadioGroupLabel>

          <div class="mt-1 grid grid-cols-1 grid-rows-3 gap-y-2">
            {testOperations.map((testOperation, index) => (
              <RadioGroupOption key={index} value={testOperation}>
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
                    <div class="flex flex-1">
                      <span class="flex flex-col">
                        <RadioGroupLabel
                          as="span"
                          class="block text-sm font-medium text-gray-900"
                        >
                          {testOperation.name}
                        </RadioGroupLabel>
                        <RadioGroupDescription
                          as="span"
                          class="mt-1 flex items-center text-sm text-gray-500"
                        >
                          {CycleSelectRadioHelperDescriptionText(
                            uiOptions.checked,
                            testOperation.description
                          )}
                        </RadioGroupDescription>
                      </span>
                    </div>
                    <div>
                      {CycleSelectRadioHelperCheckMark(uiOptions.checked)}
                    </div>
                  </div>
                )}
              </RadioGroupOption>
            ))}
          </div>
        </RadioGroup>
      );
    }

    function CycleSelectRadioHelperDescriptionText(
      isChecked: Boolean,
      descriptionText: string
    ) {
      return (
        <TransitionGroup
          enterFromClass="opacity-0"
          enterToClass="opacity-100"
          enterActiveClass="duration-[750ms]"
        >
          {() => {
            switch (isChecked) {
              case true:
                return <div key="isChecked">{descriptionText}</div>;
              default:
                return <div key="isNotChecked">&#8205;</div>;
            }
          }}
        </TransitionGroup>
      );
    }

    function CycleSelectRadioHelperCheckMark(isChecked: Boolean) {
      return (
        <TransitionGroup
          enterFromClass="opacity-0"
          enterToClass="opacity-100"
          enterActiveClass="duration-[750ms]"
        >
          {() => {
            switch (isChecked) {
              case true:
                return (
                  <CheckCircleIcon
                    key="isChecked"
                    class="h-5 w-5 text-indigo-600"
                  />
                );
              default:
                return <div class="h-5 w-5" key="isNotChecked"></div>;
            }
          }}
        </TransitionGroup>
      );
    }

    function DataMonthInput() {
      return (
        <div class="mt-6">
          <div>
            <div class="mt-2 text-sm font-medium text-gray-900 flex items-center justify-center">
              Enter Data Month
            </div>

            <input
              // ref={pafKeyInputRef}
              //@ts-ignore
              // disabled={
              //   props.buildermodule?.Status != 0 || !directoriesAvailable.value
              // }
              // v-model={pafKey.value}
              type="text"
              class="mt-2 w-full h-10 text-xs text-center rounded-md border-0 py-1.5 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-indigo-600"
              placeholder="XX"
            />
          </div>
          <div
            // ref={errorLabelRef}
            class="my-1 text-red-500 opacity-0 flex justify-center"
          >
            Invalid DataMonth
          </div>
        </div>
      );
    }

    function DataYearInput() {
      return (
        <div class="mt-6">
          <div>
            <div class="mt-2 text-sm font-medium text-gray-900 flex items-center justify-center">
              Enter Data Year
            </div>

            <input
              // ref={pafKeyInputRef}
              //@ts-ignore
              // disabled={
              //   props.buildermodule?.Status != 0 || !directoriesAvailable.value
              // }
              // v-model={pafKey.value}
              type="text"
              class="mt-2 w-full h-10 text-xs text-center rounded-md border-0 py-1.5 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-indigo-600"
              placeholder="XXXX"
            />
          </div>
          <div
            // ref={errorLabelRef}
            class="my-1 text-red-500 opacity-0 flex justify-center"
          >
            Invalid DataYear
          </div>
        </div>
      );
    }

    /* -------------------------------------------------------------------------- */
    /*                               Render function                              */
    /* -------------------------------------------------------------------------- */
    return () => (
      <div class="overflow-hidden select-none min-w-[23rem] max-w-[23rem] min-h-[12rem] bg-white rounded-lg shadow divide-y divide-gray-200">
        <div class="grid grid-cols-3 grid-rows-1 items-center px-6 pt-6 pb-1">
          <div class="col-span-2 flex items-center">
            <p class="text-gray-900 text-sm font-medium py-2">
              Directory Tester
            </p>
            {StatusLabel()}
            {StatusIcon()}
          </div>
          {DirectoryImage()}
          {OperationSelectRadio()}
          <div class="col-span-3 flex justify-between">
            {DataMonthInput()}
            {DataYearInput()}
          </div>
        </div>
        <div>
          <div class="min-h-[5rem] grid grid-cols-3 grid-rows-1 items-center">
            <div />
            {DownloadButton()}
            {CancelButton()}
          </div>
          {ProgressSlideDown()}
        </div>
      </div>
    );
  },
});
