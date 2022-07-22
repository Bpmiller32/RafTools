<script setup>
import { DocumentDownloadIcon } from "@heroicons/vue/outline";
import { onMounted, ref, watch } from "vue";
import anime from "animejs/lib/anime.es.js";
import { useStore } from "../store";

const props = defineProps(["dirType"]);
const store = useStore();

// States
const logoState = ref({
  currentIcon: null,
  icons: [
    new URL("../assets/usa.png", import.meta.url).href,
    new URL("../assets/uk.png", import.meta.url).href,
    new URL("../assets/hw.png", import.meta.url).href,
  ],
  SetIcon: () => {
    if (props.dirType == "SmartMatch") {
      logoState.value.currentIcon = logoState.value.icons[0];
    } else if (props.dirType == "Parascript") {
      logoState.value.currentIcon = logoState.value.icons[2];
    } else if (props.dirType == "RoyalMail") {
      logoState.value.currentIcon = logoState.value.icons[1];
    } else {
      logoState.value.currentIcon = "Error";
    }
  },
});
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

    for (
      let index = 0;
      index < store.crawlers[props.dirType].AvailableBuilds.length;
      index++
    ) {
      const monthNum = store.crawlers[props.dirType].AvailableBuilds[
        index
      ].Name.substring(4, 6);
      const yearNum = store.crawlers[props.dirType].AvailableBuilds[
        index
      ].Name.substring(0, 4);
      let isNew = false;

      const name =
        directoriesState.value.monthNames.get(monthNum) + " " + yearNum;
      const icon = directoriesState.value.icons.get(monthNum);

      const dateString =
        store.crawlers[props.dirType].AvailableBuilds[index].Date.split("/");
      const dirDate = new Date(
        dateString[2],
        dateString[0] - 1,
        parseInt(dateString[1])
      );

      if (dirDate >= thisMonth) {
        isNew = true;
      }

      const dir = {
        name: name,
        fileCount:
          store.crawlers[props.dirType].AvailableBuilds[index].FileCount,
        date: store.crawlers[props.dirType].AvailableBuilds[index].Date,
        time: store.crawlers[props.dirType].AvailableBuilds[index].Time,
        icon: icon,
        isNew: isNew,
      };

      directoriesState.value.directories.push(dir);
    }
  },
  EnterAni: (el, done) => {
    anime({
      targets: el,
      duration: 5000,
      delay: el.dataset.index * 500,
      opacity: [0, 0.99999],
      complete: () => {
        el.removeAttribute("style");
        done?.();
      },
    });
  },
});

// onMounted
onMounted(() => {
  logoState.value.SetIcon();
  directoriesState.value.FormatData();
});

// Watchers
watch(
  () => store.crawlers[props.dirType].AvailableBuilds.length,
  () => {
    directoriesState.value.FormatData();
  }
);
</script>

<template>
  <div class="select-none bg-white pb-4 rounded-lg shadow max-w-sm">
    <div
      class="flex justify-between items-center px-6 py-4 border-b-[1px] border-gray-400"
    >
      <div class="text-gray-900 text-sm font-medium">
        Directories Downloaded
      </div>
      <img :src="logoState.currentIcon" class="h-10 w-10" />
    </div>
    <TransitionGroup
      tag="ul"
      :css="false"
      @enter="directoriesState.EnterAni"
      appear
      class="mx-4 overflow-y-scroll max-h-40 max-w-sm divide-y divide-gray-400"
    >
      <li
        v-for="(dir, index) in directoriesState.directories"
        :key="dir.name"
        :data-index="index"
        class="px-3 py-3 flex"
      >
        <img class="h-10 w-10 rounded-full" :src="dir.icon" />
        <div class="flex items-center ml-3">
          <div>
            <div class="flex items-center">
              <p class="text-sm font-medium text-gray-900">
                {{ dir.name }} ({{ dir.fileCount }} files)
              </p>
              <DocumentDownloadIcon class="ml-2 h-5 w-5" />
              <span
                v-if="dir.isNew"
                class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800"
              >
                New
              </span>
            </div>
            <p class="text-sm text-gray-500">
              Downloaded {{ dir.date }} @ {{ dir.time }}
            </p>
          </div>
        </div>
      </li>
    </TransitionGroup>
  </div>
</template>
