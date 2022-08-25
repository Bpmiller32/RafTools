<script setup>
import { onMounted, ref, watch } from "vue";
import {
  Disclosure,
  DisclosureButton,
  DisclosurePanel,
  Listbox,
  ListboxButton,
  ListboxLabel,
  ListboxOption,
  ListboxOptions,
} from "@headlessui/vue";
import AnimationHandler from "./AnimationHandler.vue";
import anime from "animejs/lib/anime.es.js";
import {
  StatusOnlineIcon,
  ArrowCircleDownIcon,
  ExclamationCircleIcon,
  XCircleIcon,
  ChevronDoubleRightIcon,
  RefreshIcon,
  DotsHorizontalIcon,
  CheckCircleIcon,
} from "@heroicons/vue/outline";
import { CheckIcon, SelectorIcon } from "@heroicons/vue/solid";
import { useStore } from "../store";

const props = defineProps(["dirType"]);
const store = useStore();

// Template refs
const refStartButtonIcon = ref(null);

// States
const titleState = ref({
  currentTitle: null,
  titles: [
    "Tray 1: SmartMatch",
    "Tray 2: SmartMatch (Zip4 Only)",
    "Tray 3: Parascript",
    "Tray 4: RoyalMail 3.0",
    "Tray 5: RoyalMail 1.9 (Windows 7)",
    "Tray 6: RoyalMail 1.9 (Windows XP)",
  ],
  SetState: () => {
    if (props.dirType == "SmartMatch") {
      titleState.value.currentTitle = titleState.value.titles[0];
    } else if (props.dirType == "SmartMatch (Zip4 Only)") {
      titleState.value.currentTitle = titleState.value.titles[1];
    } else if (props.dirType == "Parascript") {
      titleState.value.currentTitle = titleState.value.titles[2];
    } else if (props.dirType == "RoyalMail 3.0") {
      titleState.value.currentTitle = titleState.value.titles[3];
    } else if (props.dirType == "RoyalMail 1.9 (Windows 7)") {
      titleState.value.currentTitle = titleState.value.titles[4];
    } else {
      titleState.value.currentTitle = titleState.value.titles[5];
    }
  },
});
const statusIconState = ref({
  currentIcon: null,
  icons: [
    StatusOnlineIcon,
    ArrowCircleDownIcon,
    ExclamationCircleIcon,
    XCircleIcon,
  ],
  animation: null,
  SetState: () => {
    if (store.builders[props.dirType].DirectoryStatus == "Ready") {
      statusIconState.value.currentIcon = statusIconState.value.icons[0];
    } else if (store.builders[props.dirType].DirectoryStatus == "In Progress") {
      statusIconState.value.currentIcon = statusIconState.value.icons[1];
    } else if (store.builders[props.dirType].DirectoryStatus == "Error") {
      statusIconState.value.currentIcon = statusIconState.value.icons[2];
    } else if (store.builders[props.dirType].DirectoryStatus == "Disabled") {
      statusIconState.value.currentIcon = statusIconState.value.icons[3];
    }

    statusIconState.value.animation = "FadeIn";
  },
});
const logoState = ref({
  currentIcon: null,
  icons: [
    new URL("../assets/SmartMatchLogo.png", import.meta.url).href,
    new URL("../assets/ParascriptLogo.png", import.meta.url).href,
    new URL("../assets/RoyalMailLogo.png", import.meta.url).href,
    new URL("../assets/ErrorLogo.png", import.meta.url).href,
  ],
  SetIcon: () => {
    if (props.dirType == "SmartMatch") {
      logoState.value.currentIcon = logoState.value.icons[0];
    } else if (props.dirType == "Parascript") {
      logoState.value.currentIcon = logoState.value.icons[1];
    } else if (props.dirType == "RoyalMail") {
      logoState.value.currentIcon = logoState.value.icons[2];
    } else {
      logoState.value.currentIcon = logoState.value.icons[3];
    }
  },
});
const trayLogoState = ref({
  currentIcon: null,
  icons: [
    new URL("../assets/tower1.png", import.meta.url).href,
    new URL("../assets/tower2.png", import.meta.url).href,
    new URL("../assets/tower3.png", import.meta.url).href,
    new URL("../assets/tower4.png", import.meta.url).href,
    new URL("../assets/tower5.png", import.meta.url).href,
    new URL("../assets/tower6.png", import.meta.url).href,
  ],
  SetIcon: () => {
    if (props.dirType == "SmartMatch") {
      trayLogoState.value.currentIcon = trayLogoState.value.icons[0];
    } else if (props.dirType == "SmartMatch (Zip4 Only)") {
      trayLogoState.value.currentIcon = trayLogoState.value.icons[1];
    } else if (props.dirType == "Parascript") {
      trayLogoState.value.currentIcon = trayLogoState.value.icons[2];
    } else if (props.dirType == "RoyalMail 3.0") {
      trayLogoState.value.currentIcon = trayLogoState.value.icons[3];
    } else if (props.dirType == "RoyalMail 1.9 (Windows 7)") {
      trayLogoState.value.currentIcon = trayLogoState.value.icons[4];
    } else {
      trayLogoState.value.currentIcon = trayLogoState.value.icons[5];
    }
  },
});
const statusBubbleState = ref({
  currentStatus: { status1: null, status2: null, status3: null },
  statusIcons: [" ", "✔️", "...", "❗"],
});
const runButtonState = ref({
  isActive: null,
  label: null,
  animation: null,
  SetState: () => {
    const el = refStartButtonIcon.value;
    const animation = anime({
      targets: el,
      rotate: "-=2turn",
      easing: "easeInOutSine",
      loop: true,
      autoplay: false,
    });

    if (store.builders[props.dirType].DirectoryStatus == "Ready") {
      runButtonState.value.label = "Test Disk";
      runButtonState.value.animation = "ButtonFill";
      anime.remove(el);
      runButtonState.value.isActive = true;
    } else if (store.builders[props.dirType].DirectoryStatus == "In Progress") {
      runButtonState.value.label = "Building ....";
      runButtonState.value.animation = "ButtonDrain";
      animation.play();
      runButtonState.value.isActive = false;
    } else if (store.builders[props.dirType].DirectoryStatus == "Error") {
      runButtonState.value.label = "Test Disk";
      runButtonState.value.animation = "ButtonDrain";
      anime.remove(el);
      runButtonState.value.isActive = false;
    } else if (store.builders[props.dirType].DirectoryStatus == "Disabled") {
      runButtonState.value.label = "Test Disk";
      runButtonState.value.animation = "ButtonDrain";
      anime.remove(el);
      runButtonState.value.isActive = false;
    }
  },
});

// OnMounted
onMounted(() => {
  titleState.value.SetState();
  logoState.value.SetIcon();
  trayLogoState.value.SetIcon();
  statusIconState.value.SetState();
  runButtonState.value.SetState();
});

// Events
function RunButtonClicked() {
  if (runButtonState.value.isActive == false) {
    return;
  }

  store.SendMessageBuilder(
    props.dirType,
    "Force",
    selectMenuState.value.currentSelection.name
  );
}
</script>

<template>
  <div
    class="select-none min-w-[24rem] max-w-[24rem] bg-white rounded-lg shadow"
  >
    <div class="flex items-start justify-between p-6">
      <div class="flex items-center">
        <img
          class="w-20 h-20 border rounded-full"
          :src="logoState.currentIcon"
        />
        <img class="w-14 h-14 ml-8" :src="trayLogoState.currentIcon" />
      </div>
      <div>
        <p class="text-gray-900 text-sm font-medium">
          {{ titleState.currentTitle }}
        </p>
        <div class="flex justify-start items-center mt-1">
          <AnimationHandler :animation="statusIconState.animation">
            <div
              :key="store.builders[props.dirType].DirectoryStatus"
              :class="{
                'text-green-800 bg-green-100':
                  store.builders[props.dirType].DirectoryStatus == 'Ready',
                'text-yellow-800 bg-yellow-100':
                  store.builders[props.dirType].DirectoryStatus ==
                  'In Progress',
                'text-red-800 bg-red-100':
                  store.builders[props.dirType].DirectoryStatus == 'Error',
                'text-gray-800 bg-gray-100':
                  store.builders[props.dirType].DirectoryStatus == 'Disabled',
                'px-2 py-0.5 text-xs font-medium rounded-full': true,
              }"
            >
              {{ store.builders[props.dirType].DirectoryStatus }}
            </div>
          </AnimationHandler>
          <AnimationHandler :animation="statusIconState.animation">
            <component
              :is="statusIconState.currentIcon"
              :class="{
                'text-green-500':
                  statusIconState.currentIcon == statusIconState.icons[0],
                'text-yellow-500':
                  statusIconState.currentIcon == statusIconState.icons[1],
                'text-red-500':
                  statusIconState.currentIcon == statusIconState.icons[2],
                'text-gray-500':
                  statusIconState.currentIcon == statusIconState.icons[3],
                'h-5 w-5 ml-1': true,
              }"
            ></component>
          </AnimationHandler>
        </div>
        <div class="flex justify-center items-center -space-x-7 mt-6">
          <RefreshIcon
            ref="refStartButtonIcon"
            class="shrink-0 h-5 w-5 text-white z-10"
          />
          <AnimationHandler :animation="runButtonState.animation">
            <button
              type="button"
              :key="runButtonState.isActive"
              @click="RunButtonClicked()"
              :class="{
                'bg-indigo-600 bg-[length:150%,150%] hover:bg-[length:0%,0%] hover:bg-indigo-700':
                  runButtonState.isActive == true,
                'bg-gray-500 bg-[length:0%,0%] cursor-not-allowed':
                  runButtonState.isActive == false,
                'flex shrink-0 items-center pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
              }"
            >
              {{ runButtonState.label }}
            </button>
          </AnimationHandler>
        </div>
      </div>
    </div>
    <div class="border-t-[1px] py-3 border-gray-200">
      <label class="flex justify-center text-sm font-medium text-gray-700"
        >Currently Testing: Installing Directory</label
      >
      <div class="flex justify-evenly mt-4">
        <div
          class="flex items-center justify-center min-w-[2rem] max-w-[2rem] min-h-[2rem] max-h-[2rem] px-0.5 py-0.5 text-xs font-medium rounded-full bg-green-100"
        >
          ✔️
        </div>
        <div
          class="flex items-center justify-center min-w-[2rem] max-w-[2rem] min-h-[2rem] max-h-[2rem] px-0.5 py-0.5 text-xs font-medium rounded-full bg-yellow-100"
        >
          ...
        </div>
        <div
          class="flex items-center justify-center min-w-[2rem] max-w-[2rem] min-h-[2rem] max-h-[2rem] px-0.5 py-0.5 text-xs font-medium rounded-full bg-red-100"
        >
          ❗
        </div>
        <div
          class="flex items-center justify-center min-w-[2rem] max-w-[2rem] min-h-[2rem] max-h-[2rem] px-0.5 py-0.5 text-xs font-medium rounded-full bg-gray-100"
        ></div>
      </div>
    </div>
  </div>
</template>
