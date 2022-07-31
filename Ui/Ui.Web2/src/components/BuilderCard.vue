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
} from "@heroicons/vue/outline";
import { CheckIcon, SelectorIcon } from "@heroicons/vue/solid";
import { useStore } from "../store";

const props = defineProps(["dirType"]);
const store = useStore();

// Template refs
const refStartButtonIcon = ref(null);

// States
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
      runButtonState.value.label = "Build Directory";
      runButtonState.value.animation = "ButtonFill";
      anime.remove(el);
      runButtonState.value.isActive = true;
    } else if (store.builders[props.dirType].DirectoryStatus == "In Progress") {
      runButtonState.value.label = "Building ....";
      runButtonState.value.animation = "ButtonDrain";
      animation.play();
      runButtonState.value.isActive = false;
    } else if (store.builders[props.dirType].DirectoryStatus == "Error") {
      runButtonState.value.label = "Build Directory";
      runButtonState.value.animation = "ButtonDrain";
      anime.remove(el);
      runButtonState.value.isActive = false;
    } else if (store.builders[props.dirType].DirectoryStatus == "Disabled") {
      runButtonState.value.label = "Build Directory";
      runButtonState.value.animation = "ButtonDrain";
      anime.remove(el);
      runButtonState.value.isActive = false;
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
  SetState: () => {
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
const selectMenuState = ref({
  currentSelection: null,
  menuOptions: [],
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
  SetState: () => {
    selectMenuState.value.menuOptions = [];

    // Compare AvailableBuilds in Crawler to CompiledBuilds in Builder to populate menu options array
    for (
      let i = 0;
      i < store.crawlers[props.dirType].AvailableBuilds.length;
      i++
    ) {
      const available = store.crawlers[props.dirType].AvailableBuilds[i];
      const monthNum = available.Name.substring(4, 6);
      const yearNum = available.Name.substring(0, 4);

      selectMenuState.value.menuOptions.push({
        name: available.Name,
        displayName:
          selectMenuState.value.monthNames.get(monthNum) + " " + yearNum,
        isCompiled: false,
      });

      for (
        let j = 0;
        j < store.builders[props.dirType].CompiledBuilds.length;
        j++
      ) {
        const compiled = store.builders[props.dirType].CompiledBuilds[j];

        if (available.Name == compiled.Name) {
          selectMenuState.value.menuOptions[i].isCompiled = true;
        }
      }
    }

    // Set and format the selected value to Builder's CurrentBuild
    if (
      store.builders[props.dirType].DirectoryStatus != "Ready" &&
      store.builders[props.dirType].CurrentBuild != null
    ) {
      selectMenuState.value.currentSelection = {
        name: store.builders[props.dirType].CurrentBuild,
        displayName:
          selectMenuState.value.monthNames.get(
            store.builders[props.dirType].CurrentBuild.substring(4, 6)
          ) +
          " " +
          store.builders[props.dirType].CurrentBuild.substring(0, 4),
        isCompiled: false,
      };
    }
    // Set the default selected value
    if (selectMenuState.value.menuOptions.length != 0) {
      selectMenuState.value.currentSelection =
        selectMenuState.value.menuOptions[0];
    }
  },
});

// OnMounted
onMounted(() => {
  logoState.value.SetState();
  statusIconState.value.SetState();
  runButtonState.value.SetState();
  selectMenuState.value.SetState();
});

// Watchers
// Deep watch to check store changes
watch(
  () => store.builders[props.dirType],
  () => {
    statusIconState.value.SetState();
    runButtonState.value.SetState();
    selectMenuState.value.SetState();
  },
  { deep: true }
);
watch(
  () => store.crawlers[props.dirType].AvailableBuilds.length,
  () => selectMenuState.value.SetState
);

// Events
function RunButtonClicked() {
  if (runButtonState.value.isActive == false) {
    return;
  }
  if (selectMenuState.value.currentSelection == null) {
    return;
  }

  store.SendMessageBuilder(
    props.dirType,
    "Force",
    selectMenuState.value.currentSelection.name
  );
}
function CheckboxClicked() {
  if (runButtonState.value.isActive == false) {
    return;
  }

  if (store.builders[props.dirType].AutoEnabled == true) {
    store.SendMessageBuilder(props.dirType, "AutoEnabled", "false");
  } else {
    store.SendMessageBuilder(props.dirType, "AutoEnabled", "true");
  }
}
</script>

<template>
  <div
    class="select-none min-w-[18rem] max-w-[18rem] bg-white rounded-lg shadow divide-y divide-gray-200"
  >
    <div class="p-6">
      <img
        class="w-20 h-20 ml-[33%] border rounded-full"
        :src="logoState.currentIcon"
      />
      <div class="flex justify-center mt-2 items-center shrink-0">
        <p class="text-gray-900 text-sm font-medium">
          {{ props.dirType }}
        </p>
        <AnimationHandler :animation="statusIconState.animation">
          <div
            :key="store.builders[props.dirType].DirectoryStatus"
            :class="{
              'text-green-800 bg-green-100':
                store.builders[props.dirType].DirectoryStatus == 'Ready',
              'text-yellow-800 bg-yellow-100':
                store.builders[props.dirType].DirectoryStatus == 'In Progress',
              'text-red-800 bg-red-100':
                store.builders[props.dirType].DirectoryStatus == 'Error',
              'text-gray-800 bg-gray-100':
                store.builders[props.dirType].DirectoryStatus == 'Disabled',
              'ml-3 px-2 py-0.5 text-xs font-medium rounded-full': true,
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
      <!-- UNDER CONSTRUCTION -->
      <div class="ml-5 mt-2 max-w-[18rem]">
        <Listbox as="div" v-model="selectMenuState.currentSelection">
          <ListboxLabel class="mt-2 text-sm font-medium text-gray-900">
            Select Month to Build
          </ListboxLabel>
          <div class="mt-1 relative">
            <ListboxButton
              v-if="selectMenuState.currentSelection !== null"
              :class="{
                'relative w-full max-w-[85%] bg-white border border-gray-300 rounded-md shadow-sm pl-3 pr-10 py-2 cursor-default focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500': true,
                'cursor-not-allowed': !runButtonState.isActive,
              }"
              :disabled="!runButtonState.isActive"
            >
              <div class="flex items-center">
                <div
                  v-if="!runButtonState.isActive"
                  class="bg-yellow-400 h-2 w-2 rounded-full"
                ></div>
                <div
                  v-else
                  :class="{
                    'bg-green-400':
                      selectMenuState.currentSelection.isCompiled == true,
                    'bg-gray-200':
                      selectMenuState.currentSelection.isCompiled == false,
                    'h-2 w-2 rounded-full': true,
                  }"
                ></div>
                <div class="ml-3">
                  {{ selectMenuState.currentSelection.displayName }}
                </div>
              </div>
              <span
                class="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none"
              >
                <SelectorIcon class="h-5 w-5 text-gray-400" />
              </span>
            </ListboxButton>
            <ListboxButton
              v-else
              class="relative w-full max-w-[85%] bg-white border border-gray-300 rounded-md shadow-sm px-2 py-2 cursor-default focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500"
              disabled="true"
              >No directories available</ListboxButton
            >
            <ListboxOptions
              class="absolute z-20 mt-1 w-full bg-white shadow-lg max-h-[15rem] rounded-md py-1 text-base ring-1 ring-black ring-opacity-5 overflow-auto focus:outline-none"
            >
              <ListboxOption
                as="template"
                v-for="month in selectMenuState.menuOptions"
                :key="month.name"
                :value="month"
                v-slot="{ active, selected }"
              >
                <li
                  :class="[
                    active ? 'text-white bg-indigo-600' : 'text-gray-900',
                    'cursor-default relative py-2 pl-3 pr-9',
                  ]"
                >
                  <div class="flex items-center">
                    <span
                      :class="[
                        month.isCompiled ? 'bg-green-400' : 'bg-gray-200',
                        'flex-shrink-0 h-2 w-2 rounded-full',
                      ]"
                    />
                    <span
                      :class="[
                        selected ? 'font-semibold' : 'font-normal',
                        'ml-3 truncate',
                      ]"
                    >
                      {{ month.displayName }}
                    </span>
                  </div>
                  <span
                    v-if="selected"
                    :class="[
                      active ? 'text-white' : 'text-indigo-600',
                      'absolute inset-y-0 right-0 flex items-center pr-4',
                    ]"
                  >
                    <CheckIcon class="h-5 w-5" />
                  </span>
                </li>
              </ListboxOption>
            </ListboxOptions>
          </div>
        </Listbox>
      </div>
      <!-- UNDER CONSTRUCTION -->
      <div class="flex items-center mt-2 text-gray-500 text-sm">
        <p>AutoBuild:</p>
        <input
          type="checkbox"
          v-model="store.builders[props.dirType].AutoEnabled"
          @click="CheckboxClicked()"
          :disabled="!runButtonState.isActive"
          :class="{
            'text-indigo-600 cursor-pointer': runButtonState.isActive == true,
            'text-gray-400 cursor-not-allowed':
              runButtonState.isActive == false,
            'flex items-center ml-2 h-4 w-4 focus:ring-indigo-500 border-gray-300 rounded disabled:bg-gray-400': true,
          }"
        />
      </div>
      <div class="text-gray-500 text-sm">
        <span>Next AutoBuild: </span>
        <span :key="store.builders[props.dirType].AutoDate">12:00pm</span>
        <span v-if="store.builders[props.dirType].AutoDate"> today</span>
        <span v-else> tomorrow</span>
      </div>
    </div>
    <div class="flex justify-center">
      <div>
        <div class="flex flex-1 justify-center items-center -space-x-7">
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
                'flex shrink-0 items-center my-4 pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
              }"
            >
              {{ runButtonState.label }}
            </button>
          </AnimationHandler>
        </div>
        <AnimationHandler animation="SlideDown" args="progressBar">
          <div
            v-if="runButtonState.isActive == false"
            class="overflow-hidden h-0"
          >
            <label class="flex justify-center text-sm font-medium text-gray-700"
              >Currently Building:
              {{ store.builders[props.dirType].CurrentBuild }}</label
            >
            <div
              class="min-w-[16rem] mb-4 bg-gray-200 rounded-full dark:bg-gray-700"
            >
              <div
                class="bg-indigo-600 text-xs font-medium text-indigo-100 text-center p-0.5 leading-none rounded-full"
                :style="
                  'width: ' + store.builders[props.dirType].Progress + '%'
                "
              >
                {{ store.builders[props.dirType].Progress }}%
              </div>
            </div>
          </div>
        </AnimationHandler>
      </div>
    </div>
  </div>
</template>
