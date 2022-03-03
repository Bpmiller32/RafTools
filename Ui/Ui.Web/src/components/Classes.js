class StatusResponse {
    constructor() {
        this.active = false
        this.timestamp = {
            checkinTime: "",
            checkinDate: "",
        }
        this.subdirs = {
            smartmatch: {
                status: 0,
                progress: 0,
                availableBuilds: [],
                currentBuild: ""
            },
            parascript: {
                status: 0,
                progress: 0,
                availableBuilds: [],
                currentBuild: ""
            },
            royalmail: {
                status: 0,
                progress: 0,
                availableBuilds: [],
                currentBuild: ""
            },
        }
    }
}

export default {
    StatusResponse,
}