#pragma once
#include <functional>
#include <vector>
#include <stdint.h>

enum class MessageType {
    WarningChanged,
};

class MessageBus {
public:
    template<typename Event, typename Callback>
    void subscribe(Callback cb) {
        auto& vec = getVector<Event>();
        vec.push_back(cb);
    }

    template<typename Event, typename... Args>
    void publish(Args&&... args) {
        auto& vec = getVector<Event>();
        for (auto& cb : vec)
            cb(std::forward<Args>(args)...);
    }

private:
    template<typename Event>
    std::vector<std::function<void(Event)>>& getVector() {
        static std::vector<std::function<void(Event)>> vec;
        return vec;
    }

};