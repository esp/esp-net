# Evented State Processor (ESP)

[![NuGet version](https://img.shields.io/nuget/v/esp-net.svg)](http://nuget.org/List/Packages/esp-net)  [![NuGet downloads](https://img.shields.io/nuget/dt/esp-net.svg)](http://nuget.org/List/Packages/esp-net) [![Build status](https://ci.appveyor.com/api/projects/status/2pthpwm3hb36i605/branch/master?svg=true)](https://ci.appveyor.com/project/esp/esp-net/branch/master)
[![Join the chat at https://gitter.im/esp/chat](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/esp/chat?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

ESP gives you the ability to manage changes to a model in a deterministic event driven manner.
It does this by adding specific processing workflow around changes to a model's state. 
It was born out of the need to manage complex UI and/or server state.

At its core is a `Router` which sits between event publishers and the model.
Those wanting to change the model publish events to the `Router`.
The model observes the events and applies the changes.
The model is then dispatched to model observers so new state can be applied.
It's lightweight, easy to apply and puts the model at the forefront of your design.

Get the source from [github](https://github.com/esp/esp-net) and the packages from [Nuget](https://www.nuget.org/profiles/esp).

# Help Topics

* [Installation](docs/getting-started/installation.md)
* [Examples](docs/examples/index.md)
* [Multithreading](docs/advanced-concepts/multithreading.md)
  
# JavaScript implementation

[esp-js](https://github.com/esp/esp-js), a JavaScript implementation of ESP, has more comprehensive documentation. 
The API is largely compatible, please refer to that projects documentation for more information.  