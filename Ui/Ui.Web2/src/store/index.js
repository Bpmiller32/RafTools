import { defineStore } from "pinia";

export const useStore = defineStore("counter", {
  state: () => ({
    count: 0,
    stringtest: "hello",
    connection: null,
    response: null,
  }),
});
