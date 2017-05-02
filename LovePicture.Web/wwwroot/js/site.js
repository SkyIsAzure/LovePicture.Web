var lovepic = function () {

    var now = new Date();

    return {
        getTuiJianModules: function () {
            $.post("/home/gettuijianmodules?t=" + now.getTime(), { x: 9527 }, function (data) {
                var modulesArr = [];
                if (data) {
                    if (data.isOk && data.data != null) {
                        var modules = data.data;
                        $.each(modules, function (i, item) {
                            modulesArr.push('<li><a href="/Pictures/Pictures/' + item.id + '-1">' + item.name + '</a></li>');
                        });
                    }
                }
                if (modulesArr.length > 0) {
                    $("#ul_modules").html(modulesArr.join(''));
                }
            });
        },
        getUser: function () {
            $.post("/member/getlogin?t=" + now.getTime(), function (data) {
                //console.log(data);
                var loginArr = [];
                if (data) {
                    if (data.isOk && data.data != null) {
                        var user = data.data;
                        loginArr.push('<li><a href="/usercenter/index">你好<span style="color:red">' + user.nickName + '</span></a></li>');
                        loginArr.push('<li><a href="/usercenter/index">个人中心</a></li>');
                        loginArr.push('<li><a href="/member/loginout">注销</a></li>');
                    }
                }
                if (loginArr.length > 0) {
                    $("#ul_login").html(loginArr.join(''));
                }
            });
        },
        bindSubmitBtn: function () {
            $("input[name='btnSubmit']").on("click", function () {

                var _btn = $(this);
                _btn.addClass("hide");
                var _msg = $("#msgbox");
                _msg.html("提交中，请稍后...");

                var _form = $("form[name='form_submit']");
                if (_form.valid()) {
                    _form.submit();
                } else {
                    _btn.removeClass("hide");
                    _msg.html("");
                }
            });
        },
        getUserLog: function (tabId, codeId, page, pageSize) {
            if (tabId.length <= 0 || codeId.length <= 0) { return; }
            $.post("/usercenter/userlog", { codeId: codeId, page: page, pageSize: pageSize }, function (data) {
                if (data) {

                    if (!data.isOk) { $("#" + tabId + " tbody").html('<tr><td>获取失败，稍后重试</td></tr>'); return; }
                    var trArr = [];
                    $.each(data.data, function (i, item) {
                        trArr.push('<tr><td>' + item.des + '</td></tr>');
                    });
                    if (trArr.length > 0) {
                        $("#" + tabId + " tbody").html(trArr.join(''));
                    } else {
                        $("#" + tabId + " tbody").html('<tr><td>暂无</td></tr>');
                    }
                }
            });
        },
        getUserStatis: function (tabId) {
            if (tabId.length <= 0) { return; }
            $.post("/usercenter/UserStatis", { x: 520 }, function (data) {
                if (data) {

                    if (!data.isOk) { $("#" + tabId + " tbody").html('<tr><td colspan="2">获取失败，稍后重试</td></tr>'); return; }
                    var trArr = [];
                    $.each(data.data, function (i, item) {
                        //console.log(item);
                        trArr.push('<tr><td>' + item.name + '</td><td>' + item.total + '</td></tr>');
                    });
                    if (trArr.length > 0) {
                        $("#" + tabId + " tbody").html(trArr.join(''));
                    } else {
                        $("#" + tabId + " tbody").html('<tr><td>暂无</td></tr>');
                    }
                }
            });
        },
        getUserUp: function (tabId) {
            if (tabId.length <= 0) { return; }
            $.post("/usercenter/userup", { x: 520 }, function (data) {
                if (data) {
                    if (!data.isOk) { $("#" + tabId + " tbody").html('<tr><td>获取失败，稍后重试</td></tr>'); return; }
                    var trArr = [];
                    $.each(data.data, function (i, item) {
                       
                        trArr.push('<tr><td><a href="/pictures/picview/' + item.id + '" target="_blank">' + item.name + '【浏览：' + item.readNum + '】</a></td></tr>');
                    });
                    if (trArr.length > 0) {
                        $("#" + tabId + " tbody").html(trArr.join(''));
                    } else {
                        $("#" + tabId + " tbody").html('<tr><td>暂无</td></tr>');
                    }
                }
            });
        },
        picZanOrRead: function (btnName) {
            if (btnName.length <= 0) { return; }
            $("button[name='" + btnName + "']").on("click", function () {
                var btn = $(this);
                var id = btn.attr("data-id");
                btn.addClass("disabled");
                var msg = $("#msg_" + id); msg.html("请稍等...");

                var tId = btn.attr("data-type");
                var num = $("#num_" + id);
                try {
                    $.post("/pictures/piczanorread", { id: id, tId: tId }, function (data) {
                        if (data) {
                            if (!data.isOk) {
                            } else { num.html(data.data); }
                            msg.html(data.msg);
                        }
                        btn.removeClass("disabled");
                    });
                } catch (ex) {
                    msg.html("操作失败");
                    btn.removeClass("disabled");
                }
            });
        }
    };
}

//初始化
var lovepicture = new lovepic();
//用户信息
lovepicture.getUser();
//提交按钮
lovepicture.bindSubmitBtn();
//获取推荐模块
lovepicture.getTuiJianModules();