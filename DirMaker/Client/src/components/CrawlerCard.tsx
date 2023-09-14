import { PropType, Transition, defineComponent, ref, watch } from "vue";
import BackEndObject from "../interfaces/BackEndObject";
import {
  StatusOnlineIcon,
  ArrowCircleDownIcon,
  ExclamationCircleIcon,
  ChevronDoubleRightIcon,
  RefreshIcon,
} from "@heroicons/vue/outline";
import { Disclosure, DisclosureButton, DisclosurePanel } from "@headlessui/vue";
import anime from "animejs/lib/anime.es.js";
import matwLogoSmall from "../assets/matwLogoSmall.png";

export default defineComponent({
  props: {
    name: String,
    status: Object as PropType<BackEndObject>,
  },
  setup(props) {
    const cardState = {
      Ready: 0,
      Standby: 1,
      InProgress: 2,
      Error: 3,
    };

    let renderedOnce = false;
    const editButtonRef = ref();

    let isPanelOpen: boolean;
    let ClosePanel: () => void;

    // Interactive
    function ClickEditButton() {
      if (props.status?.Status != 0) {
        ClosePanel();
        return;
      }

      let rotationDirection = [];

      if (isPanelOpen) {
        rotationDirection = [90, 0];
      } else {
        rotationDirection = [0, 90];
      }

      anime({
        targets: editButtonRef.value,
        rotate: rotationDirection,
        duration: 500,
        easing: "easeInOutSine",
      });
    }

    // Organization
    function DirectoryName() {
      return <p class="text-gray-900 text-sm font-medium">{props.name}</p>;
    }

    function DirectoryImage() {
      switch (props.name) {
        case "SmartMatch":
          return (
            <img class="w-20 h-20 border rounded-full" src={matwLogoSmall} />
          );
        case "Parascript":
          return (
            <img class="w-20 h-20 border rounded-full" src={matwLogoSmall} />
          );
        case "RoyalMail":
          return (
            <img class="w-20 h-20 border rounded-full" src={matwLogoSmall} />
          );

        default:
          return <img class="w-20 h-20 border rounded-full" />;
      }
    }

    function StatusLabel() {
      let statusLabel;

      switch (props.status?.Status) {
        case 0:
          statusLabel = "Ready";
          break;
        case 1:
          statusLabel = "Standby";
          break;
        case 2:
          statusLabel = "In Progress";
          break;
        case 3:
          statusLabel = "Error";
          break;
      }

      return (
        <Transition
          appear
          key={Number(props.status?.Status + "1")}
          enterFromClass="opacity-0"
          enterToClass="opacity-100"
          enterActiveClass="duration-[500ms]"
        >
          <div
            class={{
              "ml-3 px-2 py-0.5 text-xs font-medium rounded-full": true,
              "text-green-800 bg-green-100": props.status?.Status == 0,
              "text-yellow-800 bg-yellow-100": props.status?.Status == 2,
              "text-red-800 bg-red-100": props.status?.Status == 3,
            }}
          >
            {statusLabel}
          </div>
        </Transition>
      );
    }

    function StatusIcon() {
      return (
        <Transition
          appear
          key={Number(props.status?.Status + "2")}
          enterFromClass="opacity-0"
          enterToClass="opacity-100"
          enterActiveClass="duration-[500ms]"
        >
          <div
            class={{
              "h-5 w-5 ml-1": true,
              "text-green-500": props.status?.Status == 0,
              "text-yellow-500": props.status?.Status == 2,
              "text-red-500": props.status?.Status == 3,
            }}
          >
            {(() => {
              switch (props.status?.Status) {
                case 0:
                  return <StatusOnlineIcon />;
                case 1:
                  return <ArrowCircleDownIcon />;
                case 2:
                  return <ArrowCircleDownIcon />;
                case 3:
                  return <ExclamationCircleIcon />;
              }
            })()}
          </div>
        </Transition>
      );
    }

    function AutoSwitch() {
      return (
        <input
          type="checkbox"
          class="flex items-center ml-2 h-4 w-4 focus:ring-indigo-500 border-gray-300 rounded disabled:bg-gray-400"
        />
      );
    }

    function EditButton() {
      return (
        <Transition
          appear
          // key={Number(props.status?.Status + "0")}
          onEnter={(el: Element, done: () => void) => {
            if (props.status?.Status == 0) {
              // ButtonFill
              anime({
                targets: el,
                keyframes: [
                  { duration: 0, backgroundColor: "rgb(107 114 128)" },
                  {
                    duration: 500,
                    color: ["rgb(255 255 255)", "rgb(67 56 202)"],
                    backgroundSize: ["0% 0%", "150% 150%"],
                    easing: "easeInOutQuad",
                  },
                ],
              });
            } else {
              // ButtonDrain
              anime({
                targets: el,
                duration: 300,
                color: ["rgb(67 56 202)", "rgb(255 255 255)"],
                backgroundSize: ["150% 150%", "0% 0%"],
                easing: "easeInOutQuad",
              });
            }

            done();
          }}
        >
          <DisclosureButton
            as="button"
            disabled={props.status?.Status == 0 ? false : true}
            class={{
              "bg-gray-500 text-white bg-[length:0%,0%] cursor-not-allowed":
                props.status?.Status != 0,
              "bg-[length:0%,0%] bg-indigo-200  text-indigo-700 hover:bg-indigo-100":
                (props.status?.Status == 0) == true && isPanelOpen,
              "bg-[length:0%,0%] bg-indigo-100  text-indigo-700 hover:bg-indigo-200":
                (props.status?.Status == 0) == true && !isPanelOpen,
              "flex items-center mx-5 my-4 px-2 py-2 max-h-8 bg-gradient-to-r from-indigo-100 to-indigo-100 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md focus:outline-none":
                true,
            }}
          >
            <div class="flex items-center" onClick={() => ClickEditButton()}>
              <ChevronDoubleRightIcon
                ref={editButtonRef}
                class="h-5 w-5 mr-1"
              />
              <p>Edit AutoCrawl</p>
            </div>
          </DisclosureButton>
        </Transition>
      );
    }

    function EditPanel() {
      if (props.status?.Status != 0) {
        return;
      }

      return (
        <Transition
          appear
          enterFromClass="h-0 opacity-0"
          enterToClass="h-[6rem] opacity-100"
          enterActiveClass="duration-[250ms]"
          leaveFromClass="h-[6rem] opacity-100"
          leaveToClass="h-0 opacity-0"
          leaveActiveClass="duration-[250ms]"
        >
          <DisclosurePanel class="overflow-hidden ml-2">
            <p class="mx-1 text-sm font-medium text-gray-700 bg-blue">
              New AutoCrawl Date
            </p>
            <div class="mt-1">
              <input
                type="date"
                class="mx-1 px-2 shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 rounded-md"
                placeholder="MM/DD/YYYY"
              />
            </div>
            <p
              ref="refEditPanelLabel"
              class="mx-1 text-gray-500 mt-2 mb-4 text-sm"
            >
              Enter Date
            </p>
          </DisclosurePanel>
        </Transition>
      );
    }

    function DownloadButton() {
      return (
        <button
          type="button"
          class={{
            "flex shrink-0 items-center my-4 pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500":
              true,
          }}
        >
          Download
        </button>
      );
    }

    return () => (
      <div class="overflow-hidden select-none min-w-[23rem] max-w-[23rem] min-h-[12rem]  bg-white rounded-lg shadow divide-y divide-gray-200">
        <div class="flex items-center justify-between p-6">
          <div>
            <div class="flex items-center">
              {DirectoryName()}
              {StatusLabel()}
              {StatusIcon()}
            </div>
            <div class="flex items-center mt-2 text-gray-500 text-sm">
              <p>AutoCrawl:</p>
              {AutoSwitch()}
            </div>
            <div class="flex items-center mt-2 text-gray-500 text-sm">
              <p>Next AutoCrawl:</p>
              <p>99/99/2099</p>
            </div>
          </div>
          {DirectoryImage()}
        </div>
        <Disclosure
          as="div"
          class="flex justify-between divide-x divide-gray-200"
        >
          {({ open, close }: { open: boolean; close: () => void }) => {
            // Assign render props to component variables
            isPanelOpen = open;
            ClosePanel = close;

            return (
              <>
                <div class="flex flex-1 justify-center">
                  <div>
                    {EditButton()}
                    {EditPanel()}
                  </div>
                </div>
                <div class="flex flex-1 justify-center items-center -space-x-7">
                  <RefreshIcon class="shrink-0 h-5 w-5 text-white z-10" />
                  {DownloadButton()}
                </div>
              </>
            );
          }}
        </Disclosure>
      </div>
    );
  },
});
