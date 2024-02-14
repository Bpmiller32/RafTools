import { PropType, Transition, defineComponent, ref } from "vue";
import BackEndObject from "../interfaces/BackEndObject";
import {
  StatusOnlineIcon,
  ArrowCircleDownIcon,
  ExclamationCircleIcon,
  ChevronDoubleRightIcon,
  RefreshIcon,
} from "@heroicons/vue/outline";
import {
  Disclosure,
  DisclosureButton,
  DisclosurePanel,
  Switch,
} from "@headlessui/vue";
import matwLogoSmall from "../assets/matwLogoSmall.png";

export default defineComponent({
  props: {
    name: String,
    status: Object as PropType<BackEndObject>,
  },
  setup(props) {
    // Interactive
    function ClickDownloadButton() {}

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
      switch (props.status?.Status) {
        case 0:
          return (
            <div class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-green-800 bg-green-100">
              Ready
            </div>
          );

        case 1:
          return (
            <div class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-yellow-800 bg-yellow-100">
              Ready
            </div>
          );

        case 2:
          return (
            <div class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-red-800 bg-red-100">
              Ready
            </div>
          );
      }
    }

    function StatusIcon() {
      return (
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
      );
    }

    function AutoSwitch() {
      return (
        <Switch
          // v-model="enabled"
          class="group relative inline-flex h-5 w-10 flex-shrink-0 cursor-pointer items-center justify-center rounded-full focus:outline-none focus:ring-2 focus:ring-indigo-600 focus:ring-offset-2"
        >
          <span class="pointer-events-none absolute h-full w-full rounded-md bg-white" />
          <span class="[enabled ? 'bg-indigo-600' : 'bg-gray-200', 'pointer-events-none absolute mx-auto h-4 w-9 rounded-full transition-colors duration-200 ease-in-out']" />
          <span class="[enabled ? 'translate-x-5' : 'translate-x-0', 'pointer-events-none absolute left-0 inline-block h-5 w-5 transform rounded-full border border-gray-200 bg-white shadow ring-0 transition-transform duration-200 ease-in-out']" />
        </Switch>

        // <input
        //   type="checkbox"
        //   class="flex items-center ml-2 h-4 w-4 focus:ring-indigo-500 border-gray-300 rounded disabled:bg-gray-400"
        // />
      );
    }

    function EditButton() {
      return (
        <DisclosureButton
          as="button"
          disabled={props.status?.Status == 0 ? false : true}
          class={{
            "bg-gray-500 text-white cursor-not-allowed":
              props.status?.Status != 0,
            "bg-indigo-100  text-indigo-700 hover:bg-indigo-200":
              props.status?.Status == 0,
            "flex items-center mx-5 my-4 px-2 py-2 max-h-8 border border-transparent text-sm leading-4 font-medium rounded-md focus:outline-none":
              true,
          }}
        >
          <ChevronDoubleRightIcon class="h-5 w-5 mr-1" />
          <p class="shrink-0">Edit AutoCrawl</p>
        </DisclosureButton>
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
          disabled={props.status?.Status == 0 ? false : true}
          class={{
            "bg-gray-500 cursor-not-allowed": props.status?.Status != 0,
            "bg-indigo-600 hover:bg-indigo-700": props.status?.Status == 0,
            "flex items-center mx-10 my-4 px-2 py-2 max-h-8 border border-transparent text-sm text-white leading-4 font-medium rounded-md focus:outline-none":
              true,
          }}
        >
          <RefreshIcon class="shrink-0 h-5 w-5 text-white z-10" />
          <p class="ml-1">Download</p>
        </button>
      );
    }

    return () => (
      <div class="overflow-hidden select-none min-w-[23rem] max-w-[23rem] min-h-[12rem] bg-white rounded-lg shadow divide-y divide-gray-200">
        <div class="flex items-center justify-between p-6">
          <div>
            <div class="flex items-center">
              <p class="text-gray-900 text-sm font-medium">{props.name}</p>
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
          <div class="flex flex-1 justify-center">
            <div>
              {EditButton()}
              {EditPanel()}
            </div>
          </div>
          <div class="flex flex-1 justify-center items-center -space-x-7">
            {DownloadButton()}
          </div>
        </Disclosure>
      </div>
    );
  },
});
