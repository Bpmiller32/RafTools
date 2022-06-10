<script setup>
import { onMounted, ref, watch } from "vue";
import {
  Listbox,
  ListboxButton,
  ListboxLabel,
  ListboxOption,
  ListboxOptions,
} from "@headlessui/vue";
import {
  MailIcon,
  PhoneIcon,
  RefreshIcon,
  StatusOnlineIcon,
  ArrowCircleDownIcon,
  ExclamationCircleIcon,
  XCircleIcon,
} from "@heroicons/vue/solid";

const props = defineProps(["dirType"]);

// States
const crawlButtonState = ref({
  isActive: null,
  label: null,
  animation: null,
  SetState: () => {
    const el = refCrawlButtonIcon.value;
    const animation = anime({
      targets: el,
      rotate: "-=2turn",
      easing: "easeInOutSine",
      loop: true,
      autoplay: false,
    });

    if (autoCrawlStatus.value == "Ready") {
      crawlButtonState.value.label = "Force Crawl";
      crawlButtonState.value.animation = "ButtonFill";
      anime.remove(el);
      crawlButtonState.value.isActive = true;
    } else if (autoCrawlStatus.value == "In Progress") {
      crawlButtonState.value.label = "Crawling ....";
      crawlButtonState.value.animation = "ButtonDrain";
      animation.play();
      crawlButtonState.value.isActive = false;
    } else if (autoCrawlStatus.value == "Error") {
      crawlButtonState.value.label = "Force Crawl";
      crawlButtonState.value.animation = "ButtonDrain";
      anime.remove(el);
      crawlButtonState.value.isActive = false;
    } else if (autoCrawlStatus.value == "Disabled") {
      crawlButtonState.value.label = "Force Crawl";
      crawlButtonState.value.animation = "ButtonDrain";
      anime.remove(el);
      crawlButtonState.value.isActive = false;
    }
  },
});
const statusState = ref({
  // currentIcon: null,
  currentIcon: StatusOnlineIcon,
  icons: [
    StatusOnlineIcon,
    ArrowCircleDownIcon,
    ExclamationCircleIcon,
    XCircleIcon,
  ],
  animation: null,
  SetState: () => {
    if (autoCrawlStatus.value == "Ready") {
      statusState.value.currentIcon = statusState.value.icons[0];
    } else if (autoCrawlStatus.value == "In Progress") {
      statusState.value.currentIcon = statusState.value.icons[1];
    } else if (autoCrawlStatus.value == "Error") {
      statusState.value.currentIcon = statusState.value.icons[2];
    } else if (autoCrawlStatus.value == "Disabled") {
      statusState.value.currentIcon = statusState.value.icons[3];
    }

    statusState.value.animation = "FadeIn";
  },
});
const logoState = ref({
  currentIcon: null,
  icons: [
    new URL("../assets/SmartMatchLogo.png", import.meta.url).href,
    new URL("../assets/ParascriptLogo.png", import.meta.url).href,
    new URL("../assets/RoyalMailLogo.png", import.meta.url).href,
  ],
  SetIcon: () => {
    if (props.dirType == "SmartMatch") {
      logoState.value.currentIcon = logoState.value.icons[0];
    } else if (props.dirType == "Parascript") {
      logoState.value.currentIcon = logoState.value.icons[1];
    } else if (props.dirType == "RoyalMail") {
      logoState.value.currentIcon = logoState.value.icons[2];
    } else {
      logoState.value.currentIcon = "Error";
    }
  },
});

// OnMounted
onMounted(() => {
  logoState.value.SetIcon();
});
</script>

<template>
  <div
    class="overflow-hidden select-none min-w-[18rem] max-w-[18rem] bg-white rounded-lg shadow divide-y divide-gray-200"
  >
    <div class="flex flex-col items-center py-8">
      <img class="w-20 h-20 border rounded-full" :src="logoState.currentIcon" />
      <div class="flex mt-2">
        <p class="text-gray-900 text-sm font-medium">
          {{ props.dirType }}
        </p>
        <div
          :class="{
            'text-green-800 bg-green-100': true,
            // 'text-yellow-800 bg-yellow-100': autoCrawlStatus == 'In Progress',
            // 'text-red-800 bg-red-100': autoCrawlStatus == 'Error',
            // 'text-gray-800 bg-gray-100': autoCrawlStatus == 'Disabled',
            'ml-3 px-2 py-0.5 text-xs font-medium rounded-full': true,
          }"
        >
          Ready
        </div>
        <component
          :is="statusState.currentIcon"
          :class="{
            'text-green-500': true,
            // 'text-green-500': statusState.currentIcon == statusState.icons[0],
            // 'text-yellow-500': statusState.currentIcon == statusState.icons[1],
            // 'text-red-500': statusState.currentIcon == statusState.icons[2],
            // 'text-gray-500': statusState.currentIcon == statusState.icons[3],
            'h-5 w-5 ml-1': true,
          }"
        ></component>
      </div>

      <!-- <Listbox as="div" v-model="selected">
        <ListboxLabel class="mt-4 block text-sm font-medium text-gray-700">
          Select Directory:
        </ListboxLabel>
        <div class="mt-1">
          <ListboxButton
            class="min-w-[15rem] bg-white border border-gray-300 rounded-md shadow-sm pl-3 pr-10 py-2 text-left cursor-default focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          >
            <span class="flex items-center">
              <span
                :class="{
                  'bg-green-400': true,
                  'flex-shrink-0 inline-block h-2 w-2 rounded-full': true,
                }"
              />
              <span class="ml-3 block truncate">May 2022</span>
            </span>
            <span
              class="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none"
            >
              <SelectorIcon class="h-5 w-5 text-gray-400" />
            </span>
          </ListboxButton>

          <transition
            leave-active-class="transition ease-in duration-100"
            leave-from-class="opacity-100"
            leave-to-class="opacity-0"
          >
            <ListboxOptions
              class="absolute z-10 mt-1 w-full bg-white shadow-lg max-h-60 rounded-md py-1 text-base ring-1 ring-black ring-opacity-5 overflow-auto focus:outline-none sm:text-sm"
            >
              <ListboxOption
                as="template"
                v-for="person in people"
                :key="person.id"
                :value="person"
                v-slot="{ active, selected }"
              >
                <li
                  :class="[
                    active ? 'text-white bg-indigo-600' : 'text-gray-900',
                    'cursor-default select-none relative py-2 pl-3 pr-9',
                  ]"
                >
                  <div class="flex items-center">
                    <span
                      :class="[
                        person.online ? 'bg-green-400' : 'bg-gray-200',
                        'flex-shrink-0 inline-block h-2 w-2 rounded-full',
                      ]"
                      aria-hidden="true"
                    />
                    <span
                      :class="[
                        selected ? 'font-semibold' : 'font-normal',
                        'ml-3 block truncate',
                      ]"
                    >
                      {{ person.name }}
                      <span class="sr-only">
                        is {{ person.online ? "online" : "offline" }}</span
                      >
                    </span>
                  </div>

                  <span
                    v-if="selected"
                    :class="[
                      active ? 'text-white' : 'text-indigo-600',
                      'absolute inset-y-0 right-0 flex items-center pr-4',
                    ]"
                  >
                    <CheckIcon class="h-5 w-5" aria-hidden="true" />
                  </span>
                </li>
              </ListboxOption>
            </ListboxOptions>
          </transition>
        </div>
      </Listbox> -->
    </div>
    <div class="flex justify-center items-center -space-x-7">
      <RefreshIcon class="shrink-0 h-5 w-5 text-white z-10" />
      <button
        type="button"
        :class="{
          'bg-indigo-600 bg-[length:150%,150%] hover:bg-[length:0%,0%] hover:bg-indigo-700': true,
          'flex shrink-0 items-center my-4 pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
        }"
      >
        Build
      </button>
    </div>
  </div>
</template>
